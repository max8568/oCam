# FrameCam

類似 oCam 的 Windows 螢幕錄影工具。拖曳錄影框時有放大鏡，可以把框對到指定的像素。

- 框選範圍錄成 MP4（最高每秒 60 格），同時錄系統聲音
- 用同一個框截圖存成 PNG
- 方向鍵一次移 1 像素，Ctrl+方向鍵調整最後拖過的那條邊
- 常用尺寸、自動對齊視窗邊緣、選視窗
- 快捷鍵：F2 錄影／停止、F3 截圖（可以在設定裡改）

設計文件：`docs/superpowers/specs/2026-10-14-framecam-design.md`

## 需要

- Windows 10 2004 版以後，或 Windows 11（x64）
- .NET 10 SDK
- Visual C++ 可轉散發套件 x64（ScreenRecorderLib 需要）

## 執行

    dotnet run --project src/FrameCam

影片和截圖預設存在「影片\FrameCam」，設定檔在 `%APPDATA%\FrameCam\settings.json`。設定檔讀不出來時會改用預設值，原本的檔案另存為 `settings.json.bad`。

## 已知限制

- 影片的每一格照實際時間記錄，每秒格數不固定：畫面沒有變化時，寫進去的格數會變少，這樣聲音和畫面才對得上。少數剪輯軟體讀這種影片會有問題。
- 畫面沒有變化時按停止，最後大約 1 秒的畫面和聲音可能沒有錄進去（ScreenRecorderLib 的限制）。畫面有在動的時候不會。

## 測試

    dotnet test tests/FrameCam.Core.Tests
    dotnet test tests/FrameCam.CaptureTests

`FrameCam.CaptureTests` 會在螢幕上開視窗、播放聲音約 1 分鐘，執行時不要動滑鼠和鍵盤。錄影相關的測試需要 PATH 裡有 `ffmpeg` 和 `ffprobe`，找不到時會略過。
