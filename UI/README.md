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
| [`com.riseon.utils`](../Core/README.md) 1.0.0 | Tự cài theo `package.json` | Lớp nền, Undo, extension |
| `com.unity.ugui` 2.0.0 | Tự cài theo `package.json` | UGUI |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Attribute Inspector của các component |
| [DOTween](https://dotween.demigiant.com) | Cài tay từ Asset Store | `com.riseon.utils` cần |

"Tự cài" là khi cài qua OpenUPM; cài bằng git URL thì phải cài `com.riseon.utils`
trước. Odin và DOTween không có trên UPM nên phải cài vào project trước.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package. `com.riseon.utils` được kéo về tự
động:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.ui": "1.0.0"
  }
}
```

**Git URL**: cài `com.riseon.utils` trước (Package Manager không tự kéo phụ thuộc
qua git), rồi:

```
https://github.com/riseongamestudio/Utils.git?path=/UI#com.riseon.utils.ui/1.0.0
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

**Đổi alpha của Image**:

```csharp
using RiseOn.Utils.UI;

icon.SetAlpha(0.5f);
```

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| `AspectRatioFitterElement` | Layout element giữ tỉ lệ khung | [Runtime/AspectRatioFitterElement](Runtime/AspectRatioFitterElement/README.md) |
| `RebuildLayoutFixer` | Ép layout dựng lại khi bật | [Runtime/RebuildLayoutFixer](Runtime/RebuildLayoutFixer/README.md) |
| `ImageExtensions` | `SetAlpha` cho `Image` | [Runtime/Extensions](Runtime/Extensions/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
