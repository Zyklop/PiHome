"""
This script runs the PiApi application using a development server.
"""

from os import environ
from PiApi import app, settings

if __name__ == '__main__':
    HOST = settings.host
    PORT = settings.port
    app.run(HOST, PORT)
