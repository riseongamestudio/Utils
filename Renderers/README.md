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
| [`com.riseon.utils`](../Core/README.md) 1.0.1 | Tự cài theo `package.json` | Undo, extension |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Attribute và Inspector của các component |

"Tự cài" là khi cài qua OpenUPM; cài bằng git URL thì phải cài `com.riseon.utils`
trước. Odin không có trên UPM nên phải cài vào project trước.
Thiếu Odin thì project báo một lỗi từ `RiseOn.Utils.Renderers.Requirements`.

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
    "com.riseon.utils.renderers": "1.0.2"
  }
}
```

**Git URL**: cài `com.riseon.utils` trước (Package Manager không tự kéo phụ thuộc
qua git), rồi:

```
https://github.com/riseongamestudio/Utils.git?path=/Renderers#com.riseon.utils.renderers/1.0.2
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
