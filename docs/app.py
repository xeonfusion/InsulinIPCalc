""" InsulinIPCalc v3.0 Copyright (C) 2015-2026 John George K., xeonfusion@yahoo.com
Modified Yale insulin infusion protocol calculation with Python webassembly.
"""

import math
from dataclasses import dataclass


@dataclass
class CalculationResult:
    rate: str
    explanation: str


def round_away_from_zero(value: float, digits: int = 0) -> float:
    factor = 10 ** digits
    scaled = value * factor
    # Adds 0.5 to the absolute value, truncates, then restores the original
    # sign, so exact .5 ties round away from zero instead of to even.
    rounded = math.copysign(math.floor(abs(scaled) + 0.5), scaled)
    return rounded / factor


def round_to_nearest_half(value: float) -> float:
    # Doubles the value so 0.5 increments become whole numbers, rounds away
    # from zero (so exact ties like x.25/x.75 round up), then halves it back.
    return round_away_from_zero(value * 2) / 2


def calculate_delta(infusion_rate: float) -> float:
    if infusion_rate < 3:
        return 0.5
    if infusion_rate <= 6:
        return 1
    if infusion_rate <= 9.5:
        return 1.5
    if infusion_rate <= 14.5:
        return 2
    if infusion_rate <= 19.5:
        return 3
    if infusion_rate <= 24.5:
        return 4
    return 5


def set_rate_at(change_bs: float, infusion_rate: float, delta: float, target_unit: str) -> CalculationResult:
    unit = "mmol/L" if target_unit == "mmol" else "mg/dL"
    new_rate = infusion_rate + delta
    rate = f"{new_rate:.1f} Units/hr"
    
    displayed_change = round_away_from_zero(change_bs / 18, 2) if target_unit == "mmol" else change_bs
    change_text = f"{displayed_change:.1f}"
    explanation = (
        f"Blood glucose change is {change_text} {unit}/hr, Delta is {delta:.1f}, "
        f"Insulin infusion rate change required is by {delta:.1f} Units/hr"
    )
    if delta == 10:
        explanation += ". Consult MD"
    return CalculationResult(rate, explanation)


def reduce_1_delta(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    delta = -1 * calculate_delta(infusion_rate)
    return set_rate_at(change_bs, infusion_rate, delta, target_unit)


def reduce_2_delta(infusion_rate: float) -> CalculationResult:
    delta = -2 * calculate_delta(infusion_rate)
    new_rate = infusion_rate + delta
    return CalculationResult(
        f"HOLD infusion for 30min, then set at {new_rate:.1f} Units/hr",
        "Large blood glucose reduction, hold and reduce Insulin infusion by 2Delta",
    )


def increase_1_delta(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    delta = calculate_delta(infusion_rate)
    return set_rate_at(change_bs, infusion_rate, delta, target_unit)


def increase_2_delta(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    delta = 2 * calculate_delta(infusion_rate)
    return set_rate_at(change_bs, infusion_rate, delta, target_unit)


def range1_calculation(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    """100-139 mg/dL range."""
    if change_bs < -20:
        return reduce_2_delta(infusion_rate)
    if change_bs <= -1:
        return reduce_1_delta(change_bs, infusion_rate, target_unit)
    return set_rate_at(change_bs, infusion_rate, 0.0, target_unit)


def range2_calculation(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    """140-179 mg/dL range."""
    if change_bs > 20:
        return increase_1_delta(change_bs, infusion_rate, target_unit)
    if change_bs < -40:
        return reduce_2_delta(infusion_rate)
    if change_bs < -20:
        return reduce_1_delta(change_bs, infusion_rate, target_unit)
    return set_rate_at(change_bs, infusion_rate, 0.0, target_unit)


def range3_calculation(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    """180-249 mg/dL range."""
    if change_bs >= 40:
        return increase_2_delta(change_bs, infusion_rate, target_unit)
    if change_bs >= 0:
        return increase_1_delta(change_bs, infusion_rate, target_unit)
    if change_bs < -80:
        return reduce_2_delta(infusion_rate)
    if change_bs < -40:
        return reduce_1_delta(change_bs, infusion_rate, target_unit)
    return set_rate_at(change_bs, infusion_rate, 0.0, target_unit)


def range4_calculation(change_bs: float, infusion_rate: float, target_unit: str) -> CalculationResult:
    """>=250 mg/dL range."""
    if change_bs > 0:
        return increase_2_delta(change_bs, infusion_rate, target_unit)
    if change_bs >= -40:
        return increase_1_delta(change_bs, infusion_rate, target_unit)
    if change_bs >= -80:
        return set_rate_at(change_bs, infusion_rate, 0.0, target_unit)
    if change_bs >= -120:
        return reduce_1_delta(change_bs, infusion_rate, target_unit)
    return reduce_2_delta(infusion_rate)  # change_bs < -120


def rescue1_calculation(target_unit: str) -> CalculationResult:
    threshold = "5.6 mmol/L" if target_unit == "mmol" else "100 mg/dL"
    low = "3.9 mmol/L" if target_unit == "mmol" else "70 mg/dL"
    return CalculationResult(
        f"STOP insulin infusion & administer 1 amp (25 g) D50 IV; recheck BG q 15 minutes until ≥{threshold}. "
        f"Administer further 25g D50 IV if BG <{low}. Then, recheck BG after 1 hr; if ≥{threshold}, "
        "restart insulin infusion at 50% of most recent rate.",
        "Warning! Hypoglycemia",
    )


def rescue2_calculation(target_unit: str) -> CalculationResult:
    threshold = "5.6 mmol/L" if target_unit == "mmol" else "100 mg/dL"
    low = "3.9 mmol/L" if target_unit == "mmol" else "70 mg/dL"
    return CalculationResult(
        f"STOP insulin infusion & administer 1/2 amp (12.5 g) D50 IV; recheck BG q 15 minutes until ≥{threshold}. "
        f"Administer further 12.5g D50 IV if BG <{low}. Then, recheck BG after 1 hr; if ≥{threshold}, "
        "restart insulin infusion at 75% of most recent rate.",
        "Warning! Hypoglycemia",
    )


def rescue3_calculation(target_unit: str) -> CalculationResult:
    threshold = "5.6 mmol/L" if target_unit == "mmol" else "100 mg/dL"
    low = "3.9 mmol/L" if target_unit == "mmol" else "70 mg/dL"
    return CalculationResult(
        f"STOP insulin infusion & Recheck BG q 30 minutes until BG ≥{threshold}. "
        f"Administer further 12.5g D50 IV if BG <{low}. Restart infusion at 75% of most recent rate. "
        "Resume BG check q 1 hr.",
        "Warning! Hypoglycemia",
    )


def calculate(
    target_unit: str,
    current_glucose: float,
    previous_glucose: float,
    reading_time: float,
    infusion_rate: float,
) -> CalculationResult:
    """Return the recommended rate and protocol explanation."""
    if target_unit not in {"mgdl", "mmol"}:
        raise ValueError("Target unit must be mgdl or mmol.")
    if current_glucose < 0 or previous_glucose < 0 or infusion_rate < 0:
        raise ValueError("Glucose and infusion rate cannot be negative.")
    if reading_time <= 0:
        raise ValueError("Time between readings must be greater than zero.")

    current_bs = current_glucose * 18 if target_unit == "mmol" else current_glucose
    previous_bs = previous_glucose * 18 if target_unit == "mmol" else previous_glucose
    change_bs = (current_bs - previous_bs) / reading_time

    if (previous_bs == 0 or infusion_rate == 0) and current_bs >= 100:
        return start_bolus_and_infusion(current_bs, target_unit)

    if current_bs < 50:
        return rescue1_calculation(target_unit)
    if current_bs <= 69:
        return rescue2_calculation(target_unit)
    if current_bs <= 99:
        return rescue3_calculation(target_unit)

    if current_bs <= 139:
        return range1_calculation(change_bs, infusion_rate, target_unit)
    if current_bs <= 179:
        return range2_calculation(change_bs, infusion_rate, target_unit)
    if current_bs <= 249:
        return range3_calculation(change_bs, infusion_rate, target_unit)
    return range4_calculation(change_bs, infusion_rate, target_unit)  # current_bs >= 250


def calculate_for_browser(
    target_unit: str,
    current_glucose: float,
    previous_glucose: float,
    reading_time: float,
    infusion_rate: float,
) -> dict[str, str]:
    result = calculate(
        target_unit,
        current_glucose,
        previous_glucose,
        reading_time,
        infusion_rate,
    )
    return {"rate": result.rate, "explanation": result.explanation}


def start_bolus_and_infusion(current_bs: float, target_unit: str) -> CalculationResult:
    if current_bs < 180:
        return CalculationResult(
            "Start infusion at 0.5 Units/hr. Do not bolus",
            f"Starting blood glucose is less than {'10 mmol/L' if target_unit == 'mmol' else '180 mg/dL'}",
        )
    if current_bs < 300:
        rate = round_to_nearest_half(current_bs / 100)
        return CalculationResult(
            f"Start infusion at {rate:.1f} Units/hr. Do not bolus",
            f"Starting blood glucose is divided by {'5.5' if target_unit == 'mmol' else '100'} and rounded to nearest 0.5 unit",
        )
    # current >= 300
    bolus = round_away_from_zero(current_bs / 100)
    rate = round_to_nearest_half(current_bs / 100)
    return CalculationResult(
        f"Give {bolus:.1f} Units bolus & start infusion at {rate:.1f} Units/hr",
        f"Starting blood glucose is divided by {'5.5' if target_unit == 'mmol' else '100'}, and rounded to nearest 1 unit for bolus, and nearest 0.5 unit for infusion",
    )
