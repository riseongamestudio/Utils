# RiseOn.Utils.LitMotion

Tiện ích cho [LitMotion](https://github.com/annulusgames/LitMotion): một ease chọn được
trong Inspector, hoặc một curve tự vẽ.

Package `com.riseon.utils.litmotion`, namespace `RiseOn.Utils.LitMotion`. Xây trên
[RiseOn.Utils](../Core/README.md).

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [EaseExt](#easeext)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Drawer |
| [`com.riseon.utils`](../Core/README.md) 2.0.0 | Tự cài theo `package.json` | `RectFieldUtils` và `Section.CutLabel` cho drawer |
| [`com.annulusgames.lit-motion`](https://github.com/annulusgames/LitMotion) 2.0.2 | Tự cài theo `package.json`, từ OpenUPM | `Ease`, `EaseUtility` |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Odin không có trên UPM nên phải cài vào
project trước. Thiếu Odin thì project báo một lỗi từ
`RiseOn.Utils.LitMotion.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon`, và
registry OpenUPM với scope `com.annulusgames` vào `Packages/manifest.json`, rồi thêm
package. `com.riseon.utils` và LitMotion được kéo về tự động:

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
      "scopes": ["com.annulusgames"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.litmotion": "1.0.2"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry, nên manifest cần OpenUPM như ví
dụ trên; `com.riseon.utils` lấy từ registry RiseOn, hoặc cài bằng git URL trước. Rồi
*Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/LitMotion
```

**Thư mục local**: `"com.riseon.utils.litmotion": "file:D:/path/to/Utils/LitMotion"`.

## EaseExt

Một `Ease` của LitMotion, kèm `AnimationCurve` khi chọn `CustomAnimationCurve`.

```csharp
using LitMotion;
using RiseOn.Utils.LitMotion;

[SerializeField] private EaseExt ease = new(Ease.OutCubic);

var eased = ease.Evaluate(t);   // t từ 0 tới 1
```

Trong Inspector (Odin), cả field nằm trên một dòng; ô curve chỉ hiện cùng hàng, chia đôi
cột giá trị, khi chọn `CustomAnimationCurve`. Ô ease mở drop-down có ô tìm, cuộn tới
ease đang chọn. Curve mặc định là `EaseInOut` từ (0, 0) tới (1, 1).

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
