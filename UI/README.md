# RiseOn.Utils.UI

Tiện ích cho UGUI: giữ tỉ lệ khung trong layout group, ép layout lồng nhau dựng
lại cho đúng, và extension cho `Image`.

Package `com.riseon.utils.ui`, namespace `RiseOn.Utils.UI`. Xây trên
[RiseOn.Utils](../Core/README.md).

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Attribute Inspector của các component |
| [`com.riseon.utils`](../Core/README.md) 1.0.18 | Tự cài theo `package.json` | Undo, extension |
| `com.unity.ugui` 2.0.0 | Tự cài theo `package.json` | UGUI |
| [`com.cysharp.unitask`](https://github.com/Cysharp/UniTask) 2.5.11 | Tự cài theo `package.json`, từ OpenUPM | Chờ frame trong `AspectRatioFitterElement` |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Project đã cài UniTask bằng git URL thì
bản đó được dùng luôn, không cần scope `com.cysharp`. Odin không có trên UPM nên phải
cài vào project trước. Thiếu Odin thì project báo một lỗi từ
`RiseOn.Utils.UI.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon`, và
registry OpenUPM với scope `com.cysharp` vào `Packages/manifest.json`, rồi thêm package.
`com.riseon.utils` và UniTask được kéo về tự động:

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon"]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.ui": "1.0.6"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry, nên manifest cần OpenUPM như ví
dụ trên; `com.riseon.utils` lấy từ registry RiseOn, hoặc cài bằng git URL trước. Rồi
*Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/UI
```

**Thư mục local**: `"com.riseon.utils.ui": "file:D:/path/to/Utils/UI"`.

## Hướng dẫn nhanh

**Giữ tỉ lệ khung trong layout group**: thêm `AspectRatioFitterElement` vào
phần tử con, đặt *Aspect Ratio*, bật *Control Child Size* cho trục bị điều khiển
ở layout group cha. Khác `AspectRatioFitter` có sẵn, nó không giằng co với
layout group.

**Layout lồng nhau sai kích thước khi vừa bật**: thêm `RebuildLayoutFixer` vào
RectTransform gốc của cụm layout; nó dựng lại layout một lần ở frame đầu rồi tự
tắt.

**Chặn cú bấm xuống phần tử phía sau**: thêm `RaycastBlocker` vào một RectTransform
phủ vùng cần chặn. Nó nhận raycast nhưng không vẽ gì.

**Đổi alpha của Image**:

```csharp
using RiseOn.Utils.UI;

icon.SetColorA(0.5f);
```

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| `AspectRatioFitterElement` | Layout element giữ tỉ lệ khung | [Runtime/AspectRatioFitterElement](Runtime/AspectRatioFitterElement/README.md) |
| `RebuildLayoutFixer` | Ép layout dựng lại khi bật | [Runtime/RebuildLayoutFixer](Runtime/RebuildLayoutFixer/README.md) |
| `RaycastBlocker` | Vùng trong suốt chặn raycast | [Runtime/RaycastBlocker](Runtime/RaycastBlocker/README.md) |
| `ImageUtils` | Đổi một kênh màu của `Image` | [Runtime/Utils](Runtime/Utils/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
