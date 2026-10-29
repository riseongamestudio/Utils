# RiseOn.Utils.LitMotion

Tiện ích cho [LitMotion](https://github.com/annulusgames/LitMotion): một ease chọn được trong
Inspector, hoặc một curve tự vẽ.

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
| [`com.riseon.utils`](../Core/README.md) 1.0.12 | Tự cài theo `package.json` | `RectFieldUtils` cho drawer |
| [LitMotion](https://github.com/annulusgames/LitMotion) (`com.annulusgames.lit-motion`) 2.0.2 | Tự cài theo `package.json`, cần scope `com.annulusgames` | `Ease`, `EaseUtility` |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Drawer |

"Tự cài" là khi cài qua OpenUPM; cài bằng git URL thì phải cài `com.riseon.utils` và
LitMotion trước. Odin không có trên UPM nên phải cài vào project trước; thiếu Odin thì
project báo một lỗi từ `RiseOn.Utils.LitMotion.Requirements`.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon` và
`com.annulusgames` vào `Packages/manifest.json`, rồi thêm package. `com.riseon.utils` và
LitMotion được kéo về tự động:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon", "com.annulusgames"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.litmotion": "1.0.0"
  }
}
```

**Git URL**: cài `com.riseon.utils` và LitMotion trước (Package Manager không tự kéo phụ
thuộc qua git), rồi:

```
https://github.com/riseongamestudio/Utils.git?path=/LitMotion
```

## EaseExt

Một `Ease` của LitMotion, kèm `AnimationCurve` khi chọn `CustomAnimationCurve`.

```csharp
using LitMotion;
using RiseOn.Utils.LitMotion;

[SerializeField] private EaseExt ease = new(Ease.OutCubic);

var eased = ease.Evaluate(t);   // t từ 0 tới 1
```

Trong Inspector (Odin), cả field nằm trên một dòng; ô curve chỉ hiện cùng hàng, chia đôi cột
giá trị, khi chọn `CustomAnimationCurve`. Ô ease mở drop-down có ô tìm, cuộn tới ease đang chọn.
Curve mặc định là `EaseInOut` từ (0, 0) tới (1, 1).

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
