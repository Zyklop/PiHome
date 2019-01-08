from os import environ
from pypozyx import DeviceCoordinates, Coordinates

class Settings(object):
    """description of class"""

    def __init__(self):
        self.host = '0.0.0.0'
        self.port = 8080
        self.numLeds = 0
        self.Anchors = [DeviceCoordinates(0x6971, 1, Coordinates(3790, 4200, 730)),
               DeviceCoordinates(0x6E41, 1, Coordinates(4650, 11400, 500)),
               DeviceCoordinates(0x6E3A, 1, Coordinates(320, 900, 1300)),
               DeviceCoordinates(0x6960, 1, Coordinates(1900, 10950, 450))]
        self.dht = False

