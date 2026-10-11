# Authority PrivacyShield

**Authority PrivacyShield** is an open-source write-up and tool made for FiveM to stop invasive browser fingerprinting, local network scanning, and unnecessary device tracking.

---

## What's Going On?

Some FiveM anti-cheats run inside the game's built-in Chromium browser (CEF). Instead of just using the entitlement tokens Cfx.re already hands servers, they lean on normal browser APIs to fingerprint your machine and poke around your network.

Everything below is stuff I actually found pulling apart a LuaMP sample. I'm only speaking to what was in that sample, not making blanket claims about every anti-cheat out there.

One thing worth knowing: a couple of these signals are tied to your GPU/hardware, so they survive a Windows reinstall. Others, like the stored browser ID, reset the moment you wipe the cache or do a clean install.

### What They Track (from the sample I looked at)

1. **Canvas & audio fingerprinting**
   It draws hidden shapes on an HTML5 canvas and runs Web Audio API tests to read your GPU and audio rendering signature. The hardware-based part is the bit that sticks around after a reinstall.

2. **A stored "supercookie"**
   A random ID gets saved once in the browser's localStorage and read on every join, so you stay recognisable across sessions. Clearing the FiveM cache wipes it.

3. **WebRTC local-IP discovery**
   It uses WebRTC to grab your private local IP (like `192.168.x.x` or `10.x.x.x`) and work out your subnet.

4. **Local network sweep**
   Once it knows your subnet, it fires requests across your home network to see which devices answer back, basically mapping what's on your LAN.

---

## How PrivacyShield Helps

`PrivacyShield` is a lightweight C# launcher wrapper that passes standard Chromium flags and sets up Windows Firewall rules to block this background profiling when you start FiveM.

### What it Does

* **Passes Privacy Flags to FiveM:**
  * `--disable-reading-from-canvas`: Stops scripts from reading pixel data off HTML5 canvas elements.
  * `--blink-settings=audioContextEnabled=false`: Turns off Web Audio API fingerprinting.
  * `--disable-webrtc`: Kills WebRTC so your private local IP doesn't leak.
* **Blocks Local LAN Traffic (`netsh` Firewall Rules):**
  * Automatically sets outbound Windows Firewall rules to block FiveM from talking to private IP ranges (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`).

---

## How to Use It

1. **Download the Latest Release:**
   Head to the [Releases](#) tab on GitHub and download either `PrivacyShield.exe` (the full GUI launcher) or `PrivacyShield.bat` (the standalone batch script).

2. **Choose Your Launch Method:**
   * **GUI Utility (`PrivacyShield.exe`):** Open the app, point it to your `FiveM.exe` path, toggle your desired privacy parameters, and hit **Launch**.
   * **Batch Script (`PrivacyShield.bat`):** Run the `.bat` file directly as Administrator. It automatically applies the outbound `netsh` firewall rules to block local LAN probes and launches FiveM with all Chromium privacy flags pre-configured.

---

## Terms & Conditions Notice

**LuaMP** now forces players to accept an updated Terms and Conditions agreement before allowing entry into their servers. By accepting these terms, you essentially **waive** your right or ability to decline their invasive data collection methods. 

---

## Disclaimer

This repo and script are strictly for privacy auditing, educational purposes, and testing network security. Everything used here relies on built-in Windows commands (`netsh`) and official Chromium launch parameters.

