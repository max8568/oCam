# FrameCam 應用程式圖示

已選定 **01「焦點擷取」** 作為正式應用程式圖示；EXE 與主視窗共用此版本。

沿用目前介面的藍色 `#2563EB`，以白色擷取框與紅色 `#EF4444` 錄影圓點表達螢幕錄影用途。沒有字母或小字，縮小後仍保留辨識度。

- `preview.png`：完整設計預覽，包含淺色、深色背景的實際像素大小。
- `FrameCam.ico`：EXE／Windows 應用程式圖示，包含 16、20、24、32、40、48、64、128、256 px，32 位元透明色。
- `FrameCam.svg`：可編輯的完整圖示。
- `FrameCam-mark.svg`：透明底識別圖示，適合淺色工具列。
- `FrameCam-{size}.png`／`FrameCam-mark-{size}.png`：上述兩種形式的透明 PNG。

16–24 px 的 PNG／ICO 有加粗擷取框及放大錄影圓點，較大尺寸沿用標準向量比例。

EXE 已透過 `ApplicationIcon` 使用這組圖示；主視窗使用同一份內嵌資源。現有控制工具列是隱藏工作列按鈕的工具視窗，因此不會額外出現工作列按鈕；此圖示可用於 EXE、捷徑及 Windows 所顯示的應用程式識別。透明底 mark 另供工具列識別使用。

重新產生（開發時需 Pillow；執行程式不需要 Python）：

```powershell
python tools/generate_app_icons.py
```
