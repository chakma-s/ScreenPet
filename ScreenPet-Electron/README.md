# 🦖 ScreenPet — Electron Cross-Platform Edition

This is the cross-platform Electron version of ScreenPet, supporting **Windows, macOS, and Linux** natively with identical features and optimizations.

---

## 🛠️ Features
1. **Ultra-Low CPU Usage (< 1% CPU)**: Direct HTML5 Canvas rendering of dinosaur outlines, running asynchronously from cursor tracking to minimize CPU overhead.
2. **Dynamic Transparency**: Preprocesses sprites at startup to render paper backgrounds 100% transparent and keep outlines fine and sharp.
3. **Calibrated 35s Orbit**: Smoothly orbits your cursor at a relaxed 35-second rotation speed.
4. **Interactive Reactions**: 
   - **Keyboard Typing**: Detects typing globally on Windows, macOS, and Linux to make the dinosaur snap to the right, face left, and type.
   - **Mouse Clicking**: Shake the cursor to trigger rapid mouse-clicking bobbing.
5. **Dark Mode Integration**: Natively listens to OS dark mode changes to invert outlines to white.
6. **Autostart**: Option to start automatically with the OS.

---

## 💻 Running & Building Locally (Windows / Mac / Linux)

Ensure you have [Node.js](https://nodejs.org/) installed.

### 1. Install dependencies
```bash
cd ScreenPet-Electron
npm install
```

### 2. Run locally (for development/testing)
```bash
npm start
```

### 3. Package locally (Windows Portable EXE)
To build a single-file portable Windows executable (`dist/ScreenPet.exe`):
```bash
npm run dist
```

---

## 🚀 How to Build the macOS `.dmg` Installer for Free

Because macOS requires macOS compilers to build native system hooks, you must build the macOS installer on a Mac environment. We have set up a **GitHub Actions workflow** to do this on GitHub's free macOS build servers.

### Step 1: Initialize Git in your project folder
Open PowerShell in the root directory (`C:\Users\cshan\Desktop\MyPet\ScreenPet`) and run:
```powershell
git init
git add .
git commit -m "Add cross-platform Electron version and GitHub workflow"
```

### Step 2: Push to GitHub
1. Create a new public or private repository on GitHub named `ScreenPet`.
2. Connect your local folder and push it:
   ```powershell
   git remote add origin https://github.com/YOUR_GITHUB_USERNAME/ScreenPet.git
   git branch -M main
   git push -u origin main
   ```

### Step 3: Download the `.dmg` from GitHub Actions
1. Go to your repository page on GitHub.
2. Click the **Actions** tab.
3. Click on the active workflow run named **"Build ScreenPet Standalone Apps"**.
4. Once completed, scroll down to the **Artifacts** section at the bottom of the page.
5. Download **`screenpet-macos-latest`**, unzip it, and double-click the `.dmg` to install it on macOS!
