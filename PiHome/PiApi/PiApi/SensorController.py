"""
Routes and views for the flask application.
"""

from PiApi import app, settings
from Analog import Reader
from Environmental import DHT22
from pi_si7021 import Si7021
from flask import Request, jsonify
import time

reader = Reader.Reader()
isDht = settings.dht
if isDht:
    dht = DHT22.sensor()
else:
    si = Si7021()

@app.route('/sensors/api/analog/<int:pin>', methods=['GET'])
def analog(pin):
    value = reader.GetValue(pin)
    return jsonify({'pin': pin, 'value': value})

@app.route('/sensors/api/environment', methods=['GET'])
def temperature():
    if isDht:
      dht.trigger()
      time.sleep(0.2)
      return jsonify({'temperature': dht.temperature(), 'humidity': dht.humidity()})
    return jsonify({'temperature': si.temperature, 'humidity': si.relative_humidity})
