# 🦖 ScreenPet — Desktop Dinosaur T-Rex Pet

ScreenPet is a lightweight, transparent, always-on-top virtual screen pet designed in a hand-drawn Ghibli sketch style. It orbits your cursor, reacts to your inputs, adapts to your system theme, and has a negligible CPU and RAM footprint.

---

## 🎨 Features
1. **Interactive Dynamics**:
   - **Real-Time Keyboard Typing**: When you type anywhere on your OS, the dinosaur sits down and starts typing rapidly on its tiny laptop.
   - **Clicking Mouse**: Shaking your cursor quickly triggers the rapid mouse-clicking animation where the dinosaur holds and clicks a computer mouse.
2. **Smooth Circular Orbit**: Natural, smooth orbit within a strict 62px to 118px radius around the cursor. The speed is calibrated to a relaxed 35 seconds per orbit.
3. **Borderless sketch outline**: 
   - No rectangular borders or corners are rendered.
   - Uses luminance-to-alpha mapping to extract the drawing lines and make the body transparent so it doesn't cover your desktop content.
4. **Adaptive Dark Mode Inversion**: Detects light and dark background states to dynamically invert outline colors (white outlines on dark backgrounds, black outlines on light backgrounds).
5. **Autostart**: Option to configure the pet to launch automatically on OS startup.
6. **Ultra-Low Resource Footprint**: Optimized GDI+ blitting on Windows and async canvas rendering on Electron, maintaining CPU usage below **1%**.

---

## 📦 Download Standalone Installers
Go to the **Releases** section on the right sidebar of this repository to download the latest pre-built standalone packages:

* **Windows**: Download **`ScreenPet.exe`** (standalone portable app, no runtime installation needed).
* **macOS**: Download **`ScreenPet.dmg`** (native installer for Mac).

---

## 📂 Project Architecture
The project is divided into two implementations:

### 1. Windows Native Edition (`/ScreenPet`)
Built using **C# and .NET 8**. It utilizes low-level Win32 hooks and native DIB sections for hardware blitting to achieve the absolute lowest resource usage on Windows.
* **To run from source**:
  ```powershell
  cd ScreenPet
  dotnet run -c Release
  ```

### 2. Cross-Platform Edition (`/ScreenPet-Electron`)
Built using **Electron, HTML5 Canvas, and Node.js**. It runs natively on macOS, Linux, and Windows, using `uiohook-napi` for global OS-level event interception.
* **To run from source**:
  ```bash
  cd ScreenPet-Electron
  npm install
  npm start
  ```

---

## 🚀 How to Install on macOS

1. Download the **`ScreenPet.dmg`** file from the Releases page.
2. Double-click the `.dmg` file to mount it, and drag **ScreenPet** into your **Applications** folder.
3. **Bypassing macOS security gatekeeper** (First launch only):
   - Right-click the **ScreenPet** app in your Applications folder and select **Open**.
   - Click **Open** on the confirmation dialog.
4. **Accessibility Permissions**: When prompted, enable Accessibility permissions for ScreenPet under *System Settings > Privacy & Security > Accessibility* so the dinosaur can track your keyboard inputs and type along with you.
