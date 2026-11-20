# RiseOn.Utils.Renderers

Tiện ích cho SpriteRenderer, SpriteMask và thứ tự vẽ 2D: bám theo điểm neo của
renderer khác, đổi pivot sprite ngay trên object, raycaster cho EventSystem
sắp theo thứ tự vẽ thật, và hàm đổi từng kênh màu của `SpriteRenderer`.

Package `com.riseon.utils.renderers`, namespace `RiseOn.Utils.Renderers`. Xây
trên [RiseOn.Utils](../Core/README.md).

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
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Attribute và Inspector của các component |
| [`com.riseon.utils`](../Core/README.md) 1.0.18 | Tự cài theo `package.json` | Undo, extension |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Odin không có trên UPM nên phải cài vào
project trước. Thiếu Odin thì project báo một lỗi từ
`RiseOn.Utils.Renderers.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package. `com.riseon.utils` được kéo về tự
động:

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.utils.renderers": "1.0.5"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry: `com.riseon.utils` lấy từ
registry RiseOn như ví dụ trên, hoặc cài bằng git URL trước. Rồi *Package Manager → + →
Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/Renderers
```

**Thư mục local**: `"com.riseon.utils.renderers": "file:D:/path/to/Utils/Renderers"`.

## Hướng dẫn nhanh

**Gắn một object vào góc của sprite khác**: thêm `SpriteRendererAnchorer`, kéo
sprite đích vào *Target*, chọn *Anchor*. Object bám theo cả trong Edit mode.

**Đổi pivot sprite mà không sửa sprite**: thêm `SpriteRendererExt` vào một
GameObject; nó tạo sẵn con `Renderer`. Chọn sprite ngay trong Inspector của
component rồi chọn *Pivot* (9 điểm hoặc tùy chỉnh).

**Click đúng object nằm trên cùng**: thay `Physics2DRaycaster` trên Camera bằng
`RenderOrder2DRaycaster`. Sự kiện đi tới Collider2D được vẽ trên cùng, xét cả
SortingGroup, sorting layer và *Order in Layer*.

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| `SpriteRendererAnchorer`, `SpriteMaskAnchorer` | Bám điểm neo trên khung renderer khác | [Runtime/RendererAnchorer](Runtime/RendererAnchorer/README.md) |
| `SpriteRendererExt`, `SpriteMaskExt` | Pivot ghi đè, chỉnh renderer con từ object cha | [Runtime/RendererExt](Runtime/RendererExt/README.md) |
| `RenderOrder2DRaycaster` | Raycaster 2D theo thứ tự vẽ | [Runtime/RenderOrder2DRaycaster](Runtime/RenderOrder2DRaycaster/README.md) |
| `SpriteRendererUtils` | Đổi một kênh màu | [Runtime/Utils](Runtime/Utils/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
