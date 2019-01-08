"""
The flask application package.
"""

from flask import Flask
from PiApi import Settings

app = Flask(__name__)
settings = Settings.Settings()

import PiApi.LedController
import PiApi.PositioningController
import PiApi.SensorController
