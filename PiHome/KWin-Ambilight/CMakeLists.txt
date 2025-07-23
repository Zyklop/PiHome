cmake_minimum_required(VERSION 3.16)

project(kwin-ambilight LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED OFF)

find_package(ECM ${KF5_MIN_VERSION} REQUIRED NO_MODULE)
find_package(CURL REQUIRED)
set(CMAKE_MODULE_PATH ${ECM_MODULE_PATH})

include(KDEInstallDirs)
include(KDECMakeSettings)
include(KDECompilerSettings NO_POLICY_SCOPE)


find_package(
	Qt${QT_MAJOR_VERSION} ${QT_MIN_VERSION} CONFIG REQUIRED
		Core
		DBus
		Multimedia
)

add_executable(kwin-ambilight main.cpp)

target_link_libraries(kwin-ambilight
	Qt::DBus
	Qt::Multimedia
	CURL::libcurl
)

include(GNUInstallDirs)
install(TARGETS kwin-ambilight RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR})
install(DIRECTORY desktop/ DESTINATION /usr/share/applications FILES_MATCHING PATTERN "*.desktop")
