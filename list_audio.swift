import CoreAudio

func stringProperty(_ id: AudioDeviceID, _ selector: AudioObjectPropertySelector) -> String {
    var address = AudioObjectPropertyAddress(mSelector: selector, mScope: kAudioObjectPropertyScopeGlobal, mElement: kAudioObjectPropertyElementMain)
    var value: Unmanaged<CFString>?
    var size = UInt32(MemoryLayout<Unmanaged<CFString>?>.size)
    let status = withUnsafeMutablePointer(to: &value) { ptr in
        AudioObjectGetPropertyData(id, &address, 0, nil, &size, ptr)
    }
    if status == noErr, let value { return value.takeUnretainedValue() as String }
    return "<unavailable>"
}

var address = AudioObjectPropertyAddress(mSelector: kAudioHardwarePropertyDevices, mScope: kAudioObjectPropertyScopeGlobal, mElement: kAudioObjectPropertyElementMain)
var size: UInt32 = 0
AudioObjectGetPropertyDataSize(kAudioObjectSystemObject, &address, 0, nil, &size)
var devices = [AudioDeviceID](repeating: 0, count: Int(size) / MemoryLayout<AudioDeviceID>.size)
AudioObjectGetPropertyData(kAudioObjectSystemObject, &address, 0, nil, &size, &devices)
for id in devices {
    print("\(id) | \(stringProperty(id, kAudioObjectPropertyNameSelector)) | \(stringProperty(id, kAudioDevicePropertyDeviceUID))")
}
