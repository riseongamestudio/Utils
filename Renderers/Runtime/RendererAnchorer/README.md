# RendererAnchorer

[← RiseOn.Utils.Renderers](../../README.md)

Đặt Transform của object vào một điểm neo trên khung của renderer khác, và giữ
nguyên điểm đó khi renderer di chuyển, đổi sprite hay lật. Dùng để gắn icon,
hiệu ứng hay collider vào góc / cạnh của một sprite.

Hai bản cụ thể: `SpriteRendererAnchorer` (bám `SpriteRenderer`, tính cả
`flipX` / `flipY`) và `SpriteMaskAnchorer` (bám `SpriteMask`).

## Cách dùng

1. Thêm `SpriteRendererAnchorer` (hoặc `SpriteMaskAnchorer`) vào object cần bám.
2. Kéo renderer đích vào *Target*.
3. Chọn *Anchor*: một trong 9 điểm của `SpriteAlignment`, hoặc `Custom` rồi nhập
   *Custom Anchor* theo tỉ lệ 0–1 của khung (0, 0 là góc dưới trái).

Component chạy cả trong Edit mode, nên object bám theo ngay khi chỉnh trong
Scene view.

| Trường | Ý nghĩa |
|---|---|
| *Disable On First Enable* | Mặc định bật: lúc Play, căn vị trí một lần khi bật lần đầu rồi tự tắt để khỏi kiểm tra mỗi frame. Tắt đi nếu renderer đích di chuyển lúc chạy |
| *Target* | Renderer đích |
| *Anchor* / *Custom Anchor* | Điểm neo |

Nút *Update Pos From Anchor* căn lại ngay. Từ code:

```csharp
anchorer.Target = iconRenderer;
anchorer.Anchor = SpriteAlignment.TopRight;   // setter tự căn lại vị trí
anchorer.UpdatePosFromAnchor();               // căn lại khi cần
```

## Lưu ý

- Chỉ đặt vị trí, không đụng rotation hay scale của object bám.
- Mỗi lần đổi vị trí trong Edit mode đều ghi Undo.
- Tự làm bản cho renderer khác: kế thừa `RendererAnchorer<TRenderer>`, cài
  `GetTargetL2WMatrix()` (ma trận từ góc dưới trái khung ra world) và
  `GetTargetSize()`.
