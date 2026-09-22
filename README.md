# RiseOn Utils

Các package tiện ích dùng chung của RiseOn cho Unity. Mỗi thư mục là một package
UPM riêng, cài và đánh version độc lập.

| Thư mục | Package | Nội dung |
|---|---|---|
| [Core](Core/README.md) | `com.riseon.utils` | Singleton, Bounds2D, extension, utils, attribute cho Odin, tool Editor |
| [UI](UI/README.md) | `com.riseon.utils.ui` | Tiện ích UGUI |
| [Renderers](Renderers/README.md) | `com.riseon.utils.renderers` | Tiện ích SpriteRenderer, SpriteMask, thứ tự vẽ 2D |
| [Network](Network/README.md) | `com.riseon.utils.network` | Giờ từ mạng, kiểm tra có internet thật không |

## Phụ thuộc

| Package | Tự cài theo `package.json` | Phải cài tay (không có trên UPM) |
|---|---|---|
| `com.riseon.utils` | | [Odin Inspector](https://odininspector.com), [DOTween](https://dotween.demigiant.com) |
| `com.riseon.utils.ui` | `com.riseon.utils`, `com.unity.ugui` | Odin Inspector, DOTween |
| `com.riseon.utils.renderers` | `com.riseon.utils` | Odin Inspector, DOTween |
| `com.riseon.utils.network` | | |

Odin và DOTween là gói Asset Store nên không khai được trong `package.json`; cài
chúng vào project trước khi cài các package cần chúng.

## Cài đặt

Qua OpenUPM (scope `com.riseon`) hoặc git URL có `?path=/<Thư mục>`. Cách cài
chi tiết nằm trong README của từng package.

## Phát hành

Mỗi package có version riêng trong `package.json`. Tag có tiền tố tên package để
phân biệt trong cùng repo: `com.riseon.utils/1.0.0`, `com.riseon.utils.ui/1.0.0`,
`com.riseon.utils.renderers/1.0.0`, `com.riseon.utils.network/1.0.0`.

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
