const { app, BrowserWindow, ipcMain, screen, Tray, Menu, nativeTheme } = require('electron');
const path = require('path');
const { uIOhook } = require('uiohook-napi');

let win = null;
let tray = null;
let isPaused = false;

// Single-instance lock
const additionalData = { myKey: 'screenpet-electron-lock' };
const gotTheLock = app.requestSingleInstanceLock(additionalData);

if (!gotTheLock) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (win) {
      isPaused = false;
      win.webContents.send('pause-state', false);
      win.show();
    }
  });

  app.whenReady().then(() => {
    createWindow();
    createTray();
    startTracking();
  });
}

function createWindow() {
  win = new BrowserWindow({
    width: 100,
    height: 100,
    transparent: true,
    frame: false,
    alwaysOnTop: true,
    hasShadow: false,
    resizable: false,
    skipTaskbar: true,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  // Make the window click-through
  win.setIgnoreMouseEvents(true);

  win.loadFile('index.html');

  win.on('closed', () => {
    win = null;
  });
}

function createTray() {
  const iconPath = path.join(__dirname, 'assets', 'icon.png');
  tray = new Tray(iconPath);

  const contextMenu = Menu.buildFromTemplate([
    {
      label: '⏸  Pause Pet',
      id: 'pause-item',
      click: () => {
        isPaused = !isPaused;
        const pauseItem = contextMenu.getMenuItemById('pause-item');
        if (isPaused) {
          pauseItem.label = '▶  Resume Pet';
          win.hide();
        } else {
          pauseItem.label = '⏸  Pause Pet';
          win.show();
        }
        tray.setContextMenu(contextMenu);
        win.webContents.send('pause-state', isPaused);
      }
    },
    {
      label: '🚀  Start with OS',
      type: 'checkbox',
      checked: app.getLoginItemSettings().openAtLogin,
      click: (item) => {
        app.setLoginItemSettings({
          openAtLogin: item.checked
        });
      }
    },
    { type: 'separator' },
    {
      label: '❌  Exit ScreenPet',
      click: () => {
        app.quit();
      }
    }
  ]);

  tray.setToolTip('ScreenPet 🐾');
  tray.setContextMenu(contextMenu);

  tray.on('double-click', () => {
    const pauseItem = contextMenu.getMenuItemById('pause-item');
    pauseItem.click();
  });
}

function startTracking() {
  // Start the keyboard hook
  try {
    uIOhook.on('keydown', () => {
      if (win && !win.isDestroyed() && !isPaused) {
        win.webContents.send('keypress');
      }
    });
    uIOhook.start();
  } catch (err) {
    console.error('Failed to start uIOhook:', err);
  }

  // Poll cursor coordinates and send them to the renderer
  setInterval(() => {
    if (win && !win.isDestroyed() && !isPaused) {
      const cursor = screen.getCursorScreenPoint();
      win.webContents.send('cursor-update', cursor);
    }
  }, 33);

  // Send initial theme and track transitions
  ipcMain.on('get-initial-theme', (event) => {
    event.reply('theme-change', nativeTheme.shouldUseDarkColors);
  });

  nativeTheme.on('updated', () => {
    if (win && !win.isDestroyed()) {
      win.webContents.send('theme-change', nativeTheme.shouldUseDarkColors);
    }
  });

  // Handle move-window requests from the renderer
  ipcMain.on('move-window', (event, { x, y }) => {
    if (win && !win.isDestroyed()) {
      win.setPosition(Math.round(x), Math.round(y));
    }
  });
}

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('will-quit', () => {
  try {
    uIOhook.stop();
  } catch (err) {}
});
