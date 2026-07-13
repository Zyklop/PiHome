#define _DEBUG !NDEBUG // from CMake
//#define SAVE_PREVIEW

#include <cerrno>
#include <fcntl.h>
#include <cstring>
#include <unistd.h>
#include <sys/stat.h>
#include <cstdio>
#include <curl/curl.h>

#include <iostream>
#include <cmath>

#include <QDBusInterface>
#include <QDBusConnection>
#include <QDBusConnectionInterface>
#include <QDBusUnixFileDescriptor>
#include <QImage>


static const QString s_screenShotService = QStringLiteral("org.kde.KWin.ScreenShot2");
static const QString s_screenShotObjectPath = QStringLiteral("/org/kde/KWin/ScreenShot2");
static const QString s_screenShotInterface = QStringLiteral("org.kde.KWin.ScreenShot2");
static const QString s_screenShotMethod = QStringLiteral("CaptureScreen");
static const char *ApiPath = ":8080/led/api/customRGBB";
static const char *Protocol = "http://";

int main()
{
	int end = 400;
	int start = 1;
	unsigned char brightness = 60;
	bool flip = true;
	auto screen = "DP-3";
	double xZoneFactor = 0.1;
	double yZoneFactor = 0.0;
	double zoneWidthFactor = 1.0;
	double zoneHeightFactor = 0.3;
	auto ip = "192.168.50.20";

	auto uri = static_cast<char *>(malloc(strlen(ApiPath) + strlen(Protocol) + strlen(ip)));
	strcpy(uri, Protocol);
	strcat(uri, ip);
	strcat(uri, ApiPath);

	CURL *curl;
	CURLcode res;

	curl_global_init(CURL_GLOBAL_ALL);
	curl = curl_easy_init();
	struct curl_slist *headerlist = NULL;
	headerlist = curl_slist_append(headerlist, "Content-Type: application/octet-stream");

	int pipeFds[2];
	if (pipe2(pipeFds, O_CLOEXEC) == -1) {
		perror("Can not create pipe for screenshot.\n");
		return -1;
	}

	QVariantMap options{};
	options.insert(QStringLiteral("native-resolution"), true);
	//options.insert(QStringLiteral("include-decoration"), true);
	//options.insert(QStringLiteral("include-cursor"), true);
	QDBusInterface interface(s_screenShotService, s_screenShotObjectPath, s_screenShotInterface);

	QDBusReply<QVariantMap> rep
		= interface.call(
			s_screenShotMethod,
			screen,
			options,
			QVariant::fromValue(QDBusUnixFileDescriptor(pipeFds[1])));

#ifdef _DEBUG
	std::cout << "rep.isValid() = " << rep.isValid() << "\n";
	std::cout << "rep.error().isValid() = " << rep.error().isValid() << "\n";
	std::cout << rep.error().message().toStdString() << "\n\n";
	for (auto kv = rep.value().constKeyValueBegin(); kv != rep.value().constKeyValueEnd(); kv++)
	{
		std::cout << kv->first.toStdString() << " = (" << kv->second.typeName() << ") " << kv->second.toString().toStdString() << "\n";
	}
#endif

	if (!rep.isValid())
	{
		// TODO: Logging
		std::cerr << rep.error().message().toStdString() << "\n";
		close(pipeFds[0]);
		close(pipeFds[1]);
		return -2;
	}


	QMap<QString, QVariant> repv{rep.value()};
	int width = repv["width"].toInt();
	int height = repv["height"].toInt();
	int stride = repv["stride"].toInt();
	auto format = (QImage::Format)repv["format"].toInt();
	QString type = repv["type"].toString();
	int zoneX = static_cast<int>(std::round(height * xZoneFactor));
	int zoneY = static_cast<int>(std::round(width * yZoneFactor));
	int zoneHeight = static_cast<int>(std::round(height * zoneHeightFactor));
	int zoneWidth = static_cast<int>(std::round(width * zoneWidthFactor));

	if (type != "raw")
	{
		// TODO: Logging
		std::cerr << "Warning: screenshot format is not 'raw'. Proceeding, but data may be corrupt.\n";
	}

#ifdef SAVE_PREVIEW
	QImage img(width, height, (QImage::Format)format);
#endif

	close(pipeFds[0]);
	close(pipeFds[1]);

	pipe2(pipeFds, O_CLOEXEC);

	auto *sendBuffer = static_cast<unsigned char *>(calloc(end * 4, 1));
	int numLeds = end - start;
    auto sumBuffer = static_cast<unsigned long *>(calloc(numLeds * 3, sizeof(unsigned long)));
	int *rowPerLed = static_cast<int *>(calloc(numLeds, sizeof(int)));
	int bytesPerPixel;
	switch (format) {
		case QImage::Format_Invalid:
		case QImage::NImageFormats:
			bytesPerPixel = 0;
			break;
		case QImage::Format_Mono:
		case QImage::Format_MonoLSB:
		case QImage::Format_Indexed8:
		case QImage::Format_Alpha8:
		case QImage::Format_Grayscale8:
			bytesPerPixel = 1;
			break;
		case QImage::Format_RGB16:
		case QImage::Format_RGB555:
		case QImage::Format_RGB444:
		case QImage::Format_ARGB4444_Premultiplied:
		case QImage::Format_Grayscale16:
		case QImage::Format_BGR888:
			bytesPerPixel = 2;
			break;
		case QImage::Format_RGB666:
		case QImage::Format_ARGB8565_Premultiplied:
		case QImage::Format_ARGB6666_Premultiplied:
		case QImage::Format_ARGB8555_Premultiplied:
		case QImage::Format_RGB888:
			bytesPerPixel = 3;
			break;
		case QImage::Format_RGB32:
		case QImage::Format_ARGB32:
		case QImage::Format_ARGB32_Premultiplied:
		case QImage::Format_RGBX8888:
		case QImage::Format_RGBA8888:
		case QImage::Format_BGR30:
		case QImage::Format_A2BGR30_Premultiplied:
		case QImage::Format_RGBA8888_Premultiplied:
		case QImage::Format_RGB30:
		case QImage::Format_A2RGB30_Premultiplied:
			bytesPerPixel = 4;
			break;
		case QImage::Format_RGBX64:
		case QImage::Format_RGBA64:
		case QImage::Format_RGBA64_Premultiplied:
			bytesPerPixel = 8;
			break;
		default:
			bytesPerPixel = 0;
			break;
	}
	int colCount = width * bytesPerPixel;
	int *colMapping = static_cast<int *>(malloc(colCount * sizeof(int)));
	double factor = static_cast<double>(zoneWidth) / numLeds;

    for (int i = 0; i < width; i++)
    {
	    if (i < zoneY || i > zoneWidth + zoneY) {
	    	for (int j = 0; j < bytesPerPixel; j++) {
	    		colMapping[i * bytesPerPixel + j] = -1;
	    	}
	    }
    	else {
    		int ledNbr = static_cast<int>((i - zoneY) / factor);
    		switch (format) {
    			case QImage::Format_RGB32:
    			case QImage::Format_ARGB32:
    			case QImage::Format_ARGB32_Premultiplied:
    			case QImage::Format_ARGB8565_Premultiplied:
    			case QImage::Format_ARGB6666_Premultiplied:
    				colMapping[i * bytesPerPixel] = ledNbr * 3 + 2;
    				colMapping[i * bytesPerPixel + 1] = ledNbr * 3 + 1;
    				colMapping[i * bytesPerPixel + 2] = ledNbr * 3;
    				colMapping[i * bytesPerPixel + 3] = -1;
    				break;
    			case QImage::Format_RGB666:
    			case QImage::Format_RGB888:
    				colMapping[i * bytesPerPixel] = ledNbr * 3;
    				colMapping[i * bytesPerPixel + 1] = ledNbr * 3 + 1;
    				colMapping[i * bytesPerPixel + 2] = ledNbr * 3 + 2;
    				break;
    			case QImage::Format_RGBX8888:
    			case QImage::Format_RGBA8888:
    			case QImage::Format_RGBA8888_Premultiplied:
    				colMapping[i * bytesPerPixel + 3] = -1;
    				colMapping[i * bytesPerPixel] = ledNbr * 3;
    				colMapping[i * bytesPerPixel + 1] = ledNbr * 3 + 1;
    				colMapping[i * bytesPerPixel + 2] = ledNbr * 3 + 2;
    				break;
    			default:
    				std::cerr << "Unsupported input format.\n";
    				return -1;
    		}
    		rowPerLed[ledNbr]++;
    	}
    }

	size_t bufSize = stride;
	auto buf = malloc(bufSize);
	auto byteBuf = static_cast<unsigned char *>(buf);

	while (true) {
		rep = interface.call(
			s_screenShotMethod,
			screen,
			options,
			QVariant::fromValue(QDBusUnixFileDescriptor(pipeFds[1])));

		if (!rep.isValid())
		{
			// TODO: Logging
			std::cerr << rep.error().message().toStdString() << "\n";
			close(pipeFds[0]);
			close(pipeFds[1]);
			return -2;
		}

		size_t needToRead = stride * height;
		size_t index = 0;
		errno = 0;

		while (true)
		{
			size_t remaining = needToRead - index;
			size_t readRequestLen = (bufSize < remaining) ? bufSize : remaining;

			auto nRead = read(pipeFds[0], buf, readRequestLen);

			auto line = index / stride;
			if (line >= zoneX && line <= zoneX + zoneHeight) {
				for (int i = 0; i < nRead; i++) {
					int yPos = static_cast<int>((i + index) % stride);
					int target = colMapping[yPos];
					if (target > 0) {
						sumBuffer[target] += byteBuf[i];
#ifdef SAVE_PREVIEW
						*(img.bits() + index + i) = 128 + byteBuf[i] / 2;
					}
					else {
						*(img.bits() + index + i) = byteBuf[i];
					}
#else
					}
#endif
				}
			}
#ifdef SAVE_PREVIEW
			else {
				memcpy(img.bits() + index, buf, nRead);
			}
#endif
			if (nRead == -1)
			{
				perror("Pipe read error");
				break;
			}
			if (nRead == 0) { break; }
			index += nRead;
			//if (line > zoneX + zoneHeight) { break; }
			if (index >= needToRead) { break; }
		}

#ifdef SAVE_PREVIEW
		if (!img.save("/tmp/screenshot.png", "PNG", 100))
		{
			std::cerr << "Failed to save PNG\n";
			return -5;
		}
#endif

		for (int i = 0; i < numLeds; i++)
		{
			int mapped = flip ? end - 1 - i : i + start;
			auto numValues = static_cast<unsigned long>(zoneHeight) * rowPerLed[i];
			auto red = static_cast<unsigned char>(sumBuffer[i * 3] / numValues);
			auto green = static_cast<unsigned char>(sumBuffer[i * 3 + 1] / numValues);
			auto blue = static_cast<unsigned char>(sumBuffer[i * 3 + 2] / numValues);
			sumBuffer[i * 3] = 0;
			sumBuffer[i * 3 + 1] = 0;
			sumBuffer[i * 3 + 2] = 0;
			unsigned char maxValue = red;
			if (green > maxValue)
			{
				maxValue = green;
			}

			if (blue > maxValue)
			{
				maxValue = blue;
			}

			unsigned char b = brightness;
			if (maxValue < 50)
			{
				b /= 4;
			}
			else if (maxValue < 100)
			{
				b /= 2;
			}

			sendBuffer[mapped * 4] = red;
			sendBuffer[mapped * 4 + 1] = green;
			sendBuffer[mapped * 4 + 2] = blue;
			sendBuffer[mapped * 4 + 3] = b;
		}
		curl_easy_setopt(curl, CURLOPT_URL, uri);
		curl_easy_setopt(curl, CURLOPT_POSTFIELDS, sendBuffer);
		curl_easy_setopt(curl, CURLOPT_POSTFIELDSIZE, end * 4);
		curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headerlist);
		res = curl_easy_perform(curl);

		if (res != CURLE_OK) {
			std::cerr << "curl_easy_send() failed\n";
		}

#ifdef _DEBUG
		std::cout << "\nReceived " << index << " bytes.\n";
#endif

	}

	// Cleanup
	curl_easy_cleanup(curl);



	close(pipeFds[0]);
	close(pipeFds[1]);

	free(buf);
	curl_global_cleanup();

	return 0;
}  //main()

