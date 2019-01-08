# PiHome
Standalone Meshed Home Automation using Raspberry Pi

# Hardware
* Raspberry Pi
	* SPI and I2C has to be turned on
* https://workspace.circuitmaker.com/Projects/Details/Renato-Bosshart/PiHome
	* For the APA102 at least a levelshifter is required
* https://www.pozyx.io/
	* Not fully implemented yet

# Installation
1. Setup Raspberry using the latest raspian
	1. Others may work, too
2. Activate SPI and I2C
3. Install and configure required software according to Install.txt
4. Copy compiled binaries to the raspberry
5. Install pip-packages according to requirements.txt
6. Setup autostart according to Autostart.txt
	1. Either setup a cronjob
	2. Or use /etc/rc.local

# Implemented Features
* Preset creation
* Sensor readout
	* Si2071 Temperature and Humidity
	* Mq-x Air-Quality Sensor
	* Photoresistor
	* Other analogue sensors possible
* Data logging

# Planned features
* Module synchronisation
* Log-data visualialisation
* People tracking
	* Bluethooth RSSI trilateration with historical data to improve precision
	* Pozyx
* Automatisation
	* Pattern detection from log data
	* Machine learning with scikit-learn