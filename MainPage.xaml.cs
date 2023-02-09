/*  Insulin IP Calc v2.1
    Copyright (C) 2015-2023 John George K., encodenetapps@gmail.com
	
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public License
    as published by the Free Software Foundation; either version 3
    of the License, or (at your option) any later version.
 
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/
using static System.Net.Mime.MediaTypeNames;

namespace InsulinIPCalc;

public partial class MainPage : ContentPage
{
    int nCurrentBS;
    int nPreviousBS;
    int nTimeBS = 1;
    double nInfusionRate;
    int nBSChange;
    double nDelta;
    double nFinalDelta;

    public MainPage()
    {
        InitializeComponent();
        tSetRate.Text = "Units/hr";
        tExplain.Text = "";
        etTimeBS.Text = "1";
        pickerTargetBS.SelectedIndex = 1;
        tTargetUnit.Text = "mg/dL";
        tCurrentUnit.Text = "mg/dL";
        tPreviousUnit.Text = "mg/dL";
    }

    void OnPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex == 2)
        {
            tTargetUnit.Text = "mmol/L";
            tCurrentUnit.Text = "mmol/L";
            tPreviousUnit.Text = "mmol/L";
        }
        else
        {
            tTargetUnit.Text = "mg/dL";
            tCurrentUnit.Text = "mg/dL";
            tPreviousUnit.Text = "mg/dL";
        }
    }

    private void OnCalcRateClicked(object sender, EventArgs e)
    {
        HideKeyboard();
        ReadCurrentValues();

        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
        {
            nCurrentBS = nCurrentBS * 18;
            nPreviousBS = nPreviousBS * 18;

        }

        if ((nPreviousBS == 0 || nInfusionRate == 0) && nCurrentBS >= 100)
        {
            StartBolusAndInfusion();
            return;
        }

        nBSChange = (nCurrentBS - nPreviousBS) / nTimeBS;

        nFinalDelta = 0;
        nDelta = 0;

        if (pickerTargetBS.SelectedIndex == 0) //120 - 160 mg/dL
        {
            if (nCurrentBS >= 100 && nCurrentBS <= 119)
                Range1Calculation();
            else if (nCurrentBS >= 120 && nCurrentBS <= 159)
                Range2Calculation();
            else if (nCurrentBS >= 160 && nCurrentBS <= 199)
                Range3Calculation();
            else if (nCurrentBS >= 200)
                Range4Calculation();
            else if (nCurrentBS < 50)
                Rescue1Calculation();
            else if (nCurrentBS >= 50 && nCurrentBS <= 75)
                Rescue2Calculation();
            else if (nCurrentBS >= 75 && nCurrentBS <= 99)
                Rescue3Calculation();
        }

        if (pickerTargetBS.SelectedIndex == 1) //140 - 180 mg/dL
        {
            if (nCurrentBS >= 100 && nCurrentBS <= 139)
                Range1Calculation();
            else if (nCurrentBS >= 140 && nCurrentBS <= 179)
                Range2Calculation();
            else if (nCurrentBS >= 180 && nCurrentBS <= 249)
                Range3Calculation();
            else if (nCurrentBS >= 250)
                Range4Calculation();
            else if (nCurrentBS < 50)
                Rescue1Calculation();
            else if (nCurrentBS >= 50 && nCurrentBS <= 69)
                Rescue2Calculation();
            else if (nCurrentBS >= 70 && nCurrentBS <= 99)
                Rescue3Calculation();
        }

        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
        {

            if (nCurrentBS >= 100 && nCurrentBS <= 139)
                Range1Calculation();
            else if (nCurrentBS >= 140 && nCurrentBS <= 179)
                Range2Calculation();
            else if (nCurrentBS >= 180 && nCurrentBS <= 249)
                Range3Calculation();
            else if (nCurrentBS >= 250)
                Range4Calculation();
            else if (nCurrentBS < 50)
                Rescue1Calculation();
            else if (nCurrentBS >= 50 && nCurrentBS <= 69)
                Rescue2Calculation();
            else if (nCurrentBS >= 70 && nCurrentBS <= 99)
                Rescue3Calculation();
        }

    }

    private void OnCurrentBSChanged(object sender, TextChangedEventArgs e)
    {
        ReadCurrentBS();
    }

    private void ReadCurrentBS()
    {
        string CurrentBS = etCurrentBS.Text;
        if (CurrentBS != "") nCurrentBS = Convert.ToInt32(CurrentBS);
        else nCurrentBS = 0;

    }

    private void OnPreviousBSChanged(object sender, TextChangedEventArgs e)
    {
        ReadPreviouBS();
    }

    private void ReadPreviouBS()
    {
        string PrevBS = etPreviousBS.Text;
        if (PrevBS != "") nPreviousBS = Convert.ToInt32(PrevBS);
        else nPreviousBS = 0;

    }

    private void OnTimeBSChanged(object sender, TextChangedEventArgs e)
    {
        ReadTimeBS();
    }

    private void ReadTimeBS()
    {
        string TimeBS = etTimeBS.Text;
        if (TimeBS != "") nTimeBS = Convert.ToInt32(TimeBS);
        else nTimeBS = 1;

    }

    private void OnInfRateChanged(object sender, TextChangedEventArgs e)
    {
        ReadInfRate();
    }

    private void ReadInfRate()
    {
        string InfRate = etInfRate.Text;
        if (InfRate != "") nInfusionRate = Convert.ToDouble(InfRate);
    }

    private void ReadCurrentValues()
    {
        ReadCurrentBS();
        ReadPreviouBS();
        ReadTimeBS();
        ReadInfRate();
    }

    void Range1Calculation()
    {
        if (nBSChange >= 0)
            SetRateAt(); //no change
        else if (nBSChange <= -1 && nBSChange >= -20)
            Reduce1Delta();
        else if (nBSChange < -20)
        {   //Reduce2Delta ();
            string strRescue4;
            if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
                strRescue4 = "STOP Insulin infusion &" +
                " Recheck BG in 15 minutes to be sure BG ≥5.5 mmol/L." +
                " Then, recheck BG q 1 hr;" +
                " when ≥7.7 mmol/L," +
                " then restart infusion at 75% of most recent rate.\n";

            else
                strRescue4 =
                "STOP Insulin infusion &" +
                " Recheck BG in 15 minutes to be sure BG ≥100 mg/dL." +
                " Then, recheck BG q 1 hr;" +
                " when ≥140 mg/dL," +
                " then restart infusion at 75% of most recent rate.\n";
            tSetRate.Text = strRescue4;
            tExplain.Text = "Large blood sugar reduction, hold and reduce Insulin infusion";
        }

    }

    void Range2Calculation()
    {
        if (nBSChange > 40)
            Increase1Delta();
        else if (nBSChange >= 0 && nBSChange <= 40)
            SetRateAt(); //no change
        else if (nBSChange <= -1 && nBSChange >= -20)
            SetRateAt(); //no change
        else if (nBSChange <= -21 && nBSChange >= -40)
            Reduce1Delta();
        else if (nBSChange < -40)
            Reduce2Delta();
    }

    void Range3Calculation()
    {
        if (nBSChange > 60)
            Increase2Delta();
        else if (nBSChange >= 0 && nBSChange <= 60)
            Increase1Delta();
        else if (nBSChange <= -1 && nBSChange >= -40)
            SetRateAt(); //no change
        else if (nBSChange <= -41 && nBSChange >= -60)
            Reduce1Delta();
        else if (nBSChange < -60)
            Reduce2Delta();
    }

    void Range4Calculation()
    {
        if (nBSChange > 0)
            Increase2Delta();
        else if (nBSChange == 0)
            Increase1Delta();
        else if (nBSChange <= -1 && nBSChange >= -20)
            Increase1Delta();
        else if (nBSChange <= -21 && nBSChange >= -60)
            SetRateAt(); //no change
        else if (nBSChange <= -61 && nBSChange >= -80)
            Reduce1Delta();
        else if (nBSChange < -80)
            Reduce2Delta();

    }

    void Rescue1Calculation()
    {
        string strRescue1;
        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
            strRescue1 = "STOP Insulin infusion & administer 1 amp (25 g) D50 IV;" +
            " recheck BG q 15 minutes until ≥5.5 mmol/L." +
            " Then,recheck BG q 1 hr; when ≥7.7 mmol/L, wait 30 min," +
            " restart insulin infusion at 50% of most recent rate.\n";

        else strRescue1 =
            "STOP Insulin infusion & administer 1 amp (25 g) D50 IV;" +
            " recheck BG q 15 minutes until ≥100 mg/dL." +
            " Then,recheck BG q 1 hr; when ≥140 mg/dL, wait 30 min," +
            " restart insulin infusion at 50% of most recent rate.\n";

        tSetRate.Text = strRescue1;
        tExplain.Text = "Warning! Hypoglycemia";
    }

    void Rescue2Calculation()
    {
        string strRescue2;
        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
            strRescue2 =
            "STOP Insulin infusion & administer 1/2 amp (12.5 g) D50 IV;" +
            " recheck BG q 15 minutes until ≥5.5 mmol/L." +
            " Then,recheck BG q 1 hr; when ≥7.7 mmol/L, wait 30 min," +
            " restart insulin infusion at 50% of most recent rate.\n";
        else
            strRescue2 =
                "STOP Insulin infusion & administer 1/2 amp (12.5 g) D50 IV;" +
            " recheck BG q 15 minutes until ≥100 mg/dL." +
            " Then,recheck BG q 1 hr; when ≥140 mg/dL, wait 30 min," +
            " restart insulin infusion at 50% of most recent rate.\n";

        tSetRate.Text = strRescue2;
        tExplain.Text = "Warning! Hypoglycemia";
    }

    void Rescue3Calculation()
    {
        string strRescue3;
        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
            strRescue3 =
            "STOP Insulin infusion &" +
            " Recheck BG q 15 minutes until BG reaches or remains ≥5.5 mmol/L." +
            " Then, recheck BG q 1 hr;" +
            " when ≥7.7 mmol/L, wait 30 min," +
            " then restart infusion at 75% of most recent rate.\n";
        else
            strRescue3 =
            "STOP Insulin infusion &" +
            " Recheck BG q 15 minutes until BG reaches or remains ≥100 mg/dL." +
            " Then, recheck BG q 1 hr;" +
            " when ≥140 mg/dL, wait 30 min," +
            " then restart infusion at 75% of most recent rate.\n";

        tSetRate.Text = strRescue3;
        tExplain.Text = "Warning! Hypoglycemia";

    }

    void Reduce1Delta()
    {
        nFinalDelta = -1 * CalculateDelta();
        SetRateAt();
    }

    void Reduce2Delta()
    {
        nFinalDelta = -2 * CalculateDelta();
        double nFinalRate = (nFinalDelta + nInfusionRate);
        string sRate = string.Format("HOLD for 30min, then set at {0} Units/hr", nFinalRate);
        tSetRate.Text = sRate;
        tExplain.Text = "Large blood sugar reduction, hold and reduce Insulin infusion by 2Delta";
    }

    void Increase1Delta()
    {
        nFinalDelta = 1 * CalculateDelta();
        SetRateAt();
    }

    void Increase2Delta()
    {
        nFinalDelta = 2 * CalculateDelta();
        SetRateAt();
    }

    double CalculateDelta()
    {

        if (nInfusionRate < 3)
            nDelta = 0.5;
        else if (nInfusionRate >= 3 && nInfusionRate <= 6)
            nDelta = 1;
        else if (nInfusionRate >= 6.5 && nInfusionRate <= 9.5)
            nDelta = 1.5;
        else if (nInfusionRate >= 10 && nInfusionRate <= 14.5)
            nDelta = 2;
        else if (nInfusionRate >= 15 && nInfusionRate <= 19.5)
            nDelta = 3;
        else if (nInfusionRate >= 20)
            nDelta = 4;

        return nDelta;

    }

    void SetRateAt()
    {
        double nFinalRate = (nFinalDelta + nInfusionRate);
        string sRate = string.Format("{0} Units/hr", nFinalRate);
        tSetRate.Text = sRate;

        string exp;
        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
        {
            exp = string.Format("Blood Sugar change is {0} mmol/L/hr, Delta is {1}, Insulin infusion rate change required is by {2} Units/hr", nBSChange / 18, nDelta, nFinalDelta);
        }
        else
        {
            exp = string.Format("Blood Sugar change is {0} mg/dL/hr, Delta is {1}, Insulin infusion rate change required is by {2} Units/hr", nBSChange, nDelta, nFinalDelta);
        }

        tExplain.Text = exp;
    }

    void StartBolusAndInfusion()
    {
        double dval = Convert.ToDouble(nCurrentBS) / 100;
        double tempdval = dval * 2;
        tempdval = Math.Round(tempdval, 0, MidpointRounding.AwayFromZero);
        double nFinalRate = tempdval / 2;

        string sRate = string.Format("Give {0} Units bolus & start infusion at {0} Units/hr", nFinalRate);
        tSetRate.Text = sRate;

        string exp;
        if (pickerTargetBS.SelectedIndex == 2) //7.7 - 10 mmol/L
            exp = "Starting blood sugar is divided by 5.5 and rounded to nearest 0.5 unit";
        else
            exp = "Starting blood sugar is divided by 100 and rounded to nearest 0.5 unit";

        tExplain.Text = exp;

    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        etCurrentBS.Text = "";
        etPreviousBS.Text = "";
        etInfRate.Text = "";
        tSetRate.Text = "Units/hr";
        tExplain.Text = "";
        etTimeBS.Text = "1";
        //pickerTargetBS.SelectedIndex = 1;

    }

    public void HideKeyboard()
    {
        etCurrentBS.IsEnabled = false;
        etCurrentBS.IsEnabled = true;
        etInfRate.IsEnabled = false;
        etInfRate.IsEnabled = true;

    }
}

