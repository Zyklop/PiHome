import Adafruit_ADS1x15
import RPi.GPIO as GPIO

class Reader(object):
    """description of class"""

    def __init__(self):
        self.adc = Adafruit_ADS1x15.ADS1115()

    def GetValue(self, pin):
        return self.adc.read_adc(pin, 2/3)
