const form = document.querySelector("#calculator-form");
const targetUnit = document.querySelector("#target-unit");
const calculateButton = document.querySelector("#calculate-button");
const rateResult = document.querySelector("#rate-result");
const explanation = document.querySelector("#explanation");

document.querySelector("#reset-button").addEventListener("click", () => {
  rateResult.textContent = "Units/hr";
  explanation.textContent = "Enter readings and calculate a recommendation.";
});

const PYODIDE_BASE = "https://cdn.jsdelivr.net/pyodide/v0.26.2/full/";
const MGDL_PER_MMOL = 18;
const glucoseInputs = ["#current-glucose", "#previous-glucose"].map((id) => document.querySelector(id));
let lastUnit = targetUnit.value;

function convertGlucose(value, toUnit) {
  return toUnit === "mmol"
    ? (value / MGDL_PER_MMOL).toFixed(1)
    : String(Math.round(value * MGDL_PER_MMOL));
}

function updateUnitLabels() {
  const unit = targetUnit.value === "mmol" ? "mmol/L" : "mg/dL";
  document.querySelector("#current-unit").textContent = unit;
  document.querySelector("#previous-unit").textContent = unit;
}

targetUnit.addEventListener("change", () => {
  if (targetUnit.value !== lastUnit) {
    glucoseInputs.forEach((input) => {
      if (input.value !== "" && Number.isFinite(Number(input.value))) {
        input.value = convertGlucose(Number(input.value), targetUnit.value);
      }
    });
    lastUnit = targetUnit.value;
  }
  updateUnitLabels();
});

form.addEventListener("reset", () => {
  // The form resets the select after this event, so read it on the next tick.
  setTimeout(() => {
    lastUnit = targetUnit.value;
    updateUnitLabels();
  }, 0);
});

if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("sw.js").catch((error) => console.error("Service worker registration failed", error));
  });
}

async function loadCalculator() {
  const { loadPyodide } = await import(`${PYODIDE_BASE}pyodide.mjs`);
  const pyodide = await loadPyodide({ indexURL: PYODIDE_BASE });
  const source = await fetch("app.py").then((response) => response.text());
  pyodide.runPython(source);
  calculateButton.disabled = false;
  calculateButton.textContent = "Calculate rate";
  return pyodide;
}

const calculator = loadCalculator().catch((error) => {
  explanation.textContent = "Unable to load the WebAssembly calculator. Reload the page.";
  console.error(error);
  throw error;
});

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const pyodide = await calculator;
  const values = ["current-glucose", "previous-glucose", "reading-hours", "infusion-rate"]
    .map((id) => Number(document.querySelector(`#${id}`).value));
  try {
    const result = pyodide.globals.get("calculate_for_browser")(
      targetUnit.value, ...values
    ).toJs();
    rateResult.textContent = result.get("rate");
    explanation.textContent = result.get("explanation");
  } catch (error) {
    explanation.textContent = error.message.replace(/^ValueError:\s*/, "");
  }
});
