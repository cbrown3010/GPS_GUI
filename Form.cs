

using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Device.Location;
**
 * The Form1 file contains the events, listeners as well as functions for them.
 * The form is first constructed using the constructor which then loads the Form1_Load function
 * The rest of the functions acts as listeners which trigger specfic events.
 * There are also some supplementary functions.
 * 
 * @author Charlie Cho
 * @author 18010426
 */
namespace ComPort
{
    public partial class Form1 : Form
    {
        // global variable initializations
        // they are set here as they are used in multiple functions below
        string dataIN;     
        string sentence;
        double lat1;
        double lat2;
        double long1;
        double long2;
        double time1;
        double time2;
        char northSouth;
        char eastWest;
        double dis;
        double spd;
        double totalTime;
        double totalDistance;
        int debug = 0;

        // basic constructor that initializes and sets the minimum size of the constructor
        public Form1()
        {
            InitializeComponent();
            this.MinimumSize = new Size(360, 520);
        }

        
        // basic initilization function fo the form that sets up the application
        // it checks which ports are available and adds the corresponding ports
        // also checks the options for the options such as appending or updating data.
        private void Form1_Load(object sender, EventArgs e)
        {
            string[] availPorts = SerialPort.GetPortNames(); // list available ports
            cBoxCOMPORT.Items.AddRange(availPorts);  // add available ports to the appropriate combo box
            btnOpen.Enabled = true; // enable the button to open port
            btnClose.Enabled = false; // disable the button to close port
            chBoxAddToOldData.Checked = true;   // set default to appending new data
            chBoxAlwaysUpdate.Checked = false;  // disable updating the data to new data
        }

        // event that triggers when the open button is clicked
        private void btnOpen_Click(object sender, EventArgs e)
        {  
            try
            {
                // set the settings for the port such as baud rate, data bits, stop bits and parity bits using
                // the settings set from the releveant combo boxes.
                serialPort1.PortName = cBoxCOMPORT.Text;
                serialPort1.BaudRate = Convert.ToInt32(CBoxBaudRate.Text);
                serialPort1.DataBits = Convert.ToInt32(cBoxDataBits.Text); 
                serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cBoxStopBits.Text);  
                serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), cBoxParityBits.Text);  

                serialPort1.Open(); // open the serial port
                progressBar1.Value = 100; // set progress bar to complete
                btnOpen.Enabled = false; // disable the open button as it is now open
                btnClose.Enabled = true; // allow user to close the port
                lblStatusCom.Text = "ON"; // change status of port

            }

            // if opening the serial port fails
            catch (Exception err)
            {  
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnOpen.Enabled = true; // enable open button
                btnClose.Enabled = false; // disable close button
                lblStatusCom.Text = "OFF";  // keep port status as off
            }
        }

        // event that triggers when the close button is clicked
        private void btnClose_Click(object sender, EventArgs e)
        {
            // check to make sure the port is open
            if (serialPort1.IsOpen)      
            {
                serialPort1.Close();    // close the port
                progressBar1.Value = 0; // set the progress bar to 0
                btnOpen.Enabled = true; // enable open button
                btnClose.Enabled = false;  // disable the close button
                lblStatusCom.Text = "OFF";  // update the status of the port
            }
        }

        // event that triggers when data is received
        // the other crucial functions are called here so that whenever data is received,
        // the data is parased and updated to the problem.
private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            dataIN = serialPort1.ReadExisting(); // store data into dataIN
            this.Invoke(new EventHandler(ShowData)); // displays the raw data
            ParseData(); // parses the data into relevant variables
            CalculateData(); // calculates the speed, distance etc using variables
            this.Invoke(new EventHandler(ShowInfo)); // displays calculated data
        }

        // this function breaks down and parses the data into relevant variables
        // the indexes have been determined by using the NMEA GPGGA data table
        private void ParseData()
        {
            // store data received into temporary string
            sentence = dataIN;
            string[] parts = sentence.Split(new char[] { ',' }); // split the string for every comma
            if (parts[0] == "$GPGGA") // check to make sure it is GPGGA data
            {
                time2 = time1; // store the current time as now previous time
                time1 = Convert.ToDouble(parts[1]); // store the second index as time
                lat2 = lat1; // store previous latitude
                lat1 = (Convert.ToDouble(parts[2])/100); // store third index as latitude
                northSouth = parts[3][0]; // store fourth index as whether it is North/South
                long2 = long1; // store previous longitude
                long1 = (Convert.ToDouble(parts[4])/100); // store fifth index as longitude
                eastWest = parts[5][0]; // store sixth index as whether it is East/West
            }
        }

        // calculates distance, speed and keeps track of total time and distance
        private void CalculateData()
        {
            // debug is used to prevent errors occuring from storing not yet set current values as previous values
            debug++;
            if (debug > 10)
            {
                // to use the GetDistanceTo function the co-ordinates muts be GeoCoordinate objects.
                GeoCoordinate gpsPoint1 = new GeoCoordinate(lat1, long1);
                GeoCoordinate gpsPoint2 = new GeoCoordinate(lat2, long2);

                dis = gpsPoint1.GetDistanceTo(gpsPoint2); // use the GetDistanceTo to compute distance
                spd = dis / (time1 - time2); // speed is simply the distance divided by the difference in time
                totalTime += ((time1 - time2) / 60); // the total time is kept in minutes therefore divided by 60
                totalDistance += (dis/1000); // total distance is kept in km, therefore divided by 1000
            }
        }

        // displays the new calculated and or formatted data
        private void ShowInfo(object sender, EventArgs e)
        {
            // shift current data to old data before updating
            textBoxOldLat.Text = textBoxLat.Text;
            textBoxOldLong.Text = textBoxLong.Text;
            textBoxOldTime.Text = textBoxTime.Text;
            // set latitude, longtitude and time using parsed data
            textBoxLat.Text = Convert.ToString(northSouth) + Convert.ToString(lat1);
            textBoxLong.Text = Convert.ToString(eastWest) + Convert.ToString(long1);
            textBoxTime.Text = Convert.ToString(time1);
            // set distance, speed and bearing using computed methods
            textBoxDistance.Text = Convert.ToString(Math.Round(dis, 2));
            textBoxSpeed.Text = Convert.ToString(Math.Round(spd, 2));
            textBoxCompass.Text = Convert.ToString(northSouth) + Convert.ToString(eastWest);
            // set total distance and time using tallied data
            textBoxTotalDistance.Text = Convert.ToString(Math.Round(totalDistance, 4));
            textBoxTotalTime.Text = Convert.ToString(Math.Round(totalTime, 4));
            // update the speed graph using current speed and total time 
            speed.Series["liveSpeed"].Points.AddXY(Math.Round(totalTime, 2), spd);
            // update the latitude and longitude for the virtualization
            textBoxVirtualLat.Text = Convert.ToString(northSouth) + Convert.ToString(lat1);
            textBoxVirtualLong.Text = Convert.ToString(eastWest) + Convert.ToString(long1);
        }

        // displays raw data received from the port to the application
        private void ShowData(object sender, EventArgs e) 
        {
            int dataINLength = dataIN.Length; // get length of data
            lblDataInLength.Text = string.Format("{0:00}", dataINLength); // update the data length label
            // if the always update button is checked
            if (chBoxAlwaysUpdate.Checked)
            {
                tBoxDataIN.Text = dataIN; // update and replace with new data
                currentBox.Text = dataIN;
            }
            // if the add to old data option is checked
            else if (chBoxAddToOldData.Checked)
            {
                tBoxDataIN.Text += dataIN; // keep and append to the old data with new data
                currentBox.Text = dataIN;
            }
        }

        // event for checkbox, updating and replacing the old data
        private void chBoxAlwaysUpdate_CheckedChanged(object sender, EventArgs e)
        {
            if(chBoxAlwaysUpdate.Checked)
            {
                chBoxAlwaysUpdate.Checked = true;  
                chBoxAddToOldData.Checked = false;
            }
            else { chBoxAddToOldData.Checked = true; }
        }

        // event for checkbox, keeping and appending to the old data with new data
        private void chBoxAddToOldData_CheckedChanged(object sender, EventArgs e)
        {
            if(chBoxAddToOldData.Checked)
            {
                chBoxAlwaysUpdate.Checked = false;
                chBoxAddToOldData.Checked = true;   
            }
            else { chBoxAlwaysUpdate.Checked = true; }
        }

        private void btnClearDataIN_Click(object sender, EventArgs e)
        {
            if(tBoxDataIN.Text != "")   //if the received data textbox is not empty
            {
                tBoxDataIN.Text = "";   //delete all the text in the received data textbox
            }
        }
    }
}