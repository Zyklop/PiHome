from pypozyx import (PozyxConstants, Coordinates, POZYX_SUCCESS, POZYX_ANCHOR_SEL_AUTO, version,
                    DeviceCoordinates, PozyxSerial, get_first_pozyx_serial_port, SingleRegister, UWBSettings,
                    EulerAngles, Data, Pressure, Temperature, DeviceList, NetworkID)

class Controller(object):
    """description of class"""

    def CheckStatus(self, status):
        if status == PozyxConstants.STATUS_FAILURE:
            raise ConnectionError('Pozyx function failed')
        if status == PozyxConstants.STATUS_TIMEOUT:
            raise ConnectionError('Pozyx function timed out')

    def __init__(self, anchors):

        self.anchors = [DeviceCoordinates()]
        self.anchors = anchors

        self.port = get_first_pozyx_serial_port()
        if self.port is None:
            self.error = "No Pozyx connected"
            return

        self.pozyx = PozyxSerial(self.port)
        networkId = NetworkID()
        status = self.pozyx.getNetworkId(networkId)
        self.id = networkId.id
        self.CheckStatus(status)
        self.ConfigureAnchor(self.id)

    def ConfigureAnchor(self, anchorId):
        for devCoords in self.anchors:
            if devCoords.network_id == anchorId:
                position = devCoords.pos
                break
        if anchorId == self.id:
            anchorId = None
        status = self.pozyx.setCoordinates(position, anchorId)
        self.CheckStatus(status)

    def ConfigureTag(self, tagId):
        for anchor in self.anchors:
            status = self.pozyx.addDevice(anchor, tagId)
            self.CheckStatus(status)
        status = self.pozyx.configureAnchors(self.anchors, PozyxConstants.ANCHOR_SELECT_AUTO, tagId)
        self.CheckStatus(status)
        mode = SingleRegister(PozyxConstants.GPIO_DIGITAL_INPUT)
        pullup = SingleRegister(PozyxConstants.GPIO_PULL_UP)
        status = self.pozyx.setConfigGPIO(1, mode, pullup, tagId)
        self.CheckStatus(status)
        status = self.pozyx.setConfigGPIO(2, mode, pullup, tagId)
        self.CheckStatus(status)
        status = self.pozyx.setConfigGPIO(3, mode, pullup, tagId)
        self.CheckStatus(status)
        status = self.pozyx.setConfigGPIO(4, mode, pullup, tagId)
        self.CheckStatus(status)

    def ConfigureUWB(self, id, txPower = 33.0, channel = 1, bitrate = 0, gain = 67, prf = 2, preamble = 40):
        if txPower < 0.0 or txPower > 33.0:
            raise ValueError("Invalid txPower")
        if id == self.id:
            id = None
        settings = UWBSettings()
        settings.bitrate = bitrate
        settings.channel = channel
        settings.gain_db = gain
        settings.plen = preamble
        settings.prf = prf
        status = self.pozyx.setUWBSettings(settings, id)
        self.CheckStatus(status)
        status = self.pozyx.setTxPower(txPower, id)
        self.CheckStatus(status)

    def GetError(self):
        code = SingleRegister()
        status = self.pozyx.getErrorCode(code)
        self.CheckStatus(status)
        return self.pozyx.getErrorMessage(code)

    def GetCalibrationStatus(self, tagId):
        result = SingleRegister()
        status = self.pozyx.getCalibrationStatus(result, tagId)
        self.CheckStatus(status)
        value = dict()
        value['magnetic'] = result.value & 3
        value['accelerometer'] = (result.value & 12) >> 2
        value['gyroscope'] = (result.value & 48) >> 4
        value['system'] = (result.value & 192) >> 6
        return value

    def GetGPIO(self, tagId, pinId):
        value = SingleRegister()
        status = self.pozyx.getGPIO(pinId, value, tagId)
        self.CheckStatus(status)
        return value.value == 1

    def GetPosition(self, tagId):
        target = Coordinates()
        status = self.pozyx.doPositioning(target, PozyxConstants.DIMENSION_3D, algorithm = PozyxConstants.POSITIONING_ALGORITHM_UWB_ONLY, remote_id = tagId)
        self.CheckStatus(status)
        result = dict()
        result['x'] = target.x
        result['y'] = target.y
        result['z'] = target.z
        return target

    def GetEulerAngles(self, tagId):
        result = EulerAngles()
        status = self.pozyx.getEulerAngles_deg(result, tagId)
        self.CheckStatus(status)
        res = dict()
        res['heading'] = result.heading
        res['roll'] = result.roll
        res['pitch'] = result.pitch
        return res

    def GetPressure(self, tagId):
        data = Pressure()
        status = self.pozyx.getPressure_Pa(data, tagId)
        self.CheckStatus(status)
        return {'pressure' : data.value}

    def GetTemperature(self, tagId):
        data = Temperature()
        status = self.pozyx.getTemperature_c(data, tagId)
        self.CheckStatus(status)
        return {'temperature' : data.value}

    def DiscoverTags(self):
        status = self.pozyx.doDiscovery(PozyxConstants.DISCOVERY_TAGS_ONLY)
        self.CheckStatus(status)
        count = SingleRegister()
        status = self.pozyx.getDeviceListSize(count)
        self.CheckStatus(status)
        allIds = DeviceList(list_size = count.value)
        status = self.pozyx.getDeviceIds(allIds)
        self.CheckStatus(status)
        tagIds = list()
        for id in allIds:
            any = False
            for tagId in self.anchors:
                if tagId.network_id == id:
                    any = True
            if not any:
                tagIds.append(id)
        return tagIds

