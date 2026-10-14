# RaycastBlocker

[← RiseOn.Utils.UI](../../README.md)

Vùng trong suốt nhận raycast nhưng không vẽ gì. Dùng để chặn cú bấm xuống các
phần tử phía sau (nền của popup, lớp chặn lúc chờ) mà không cần một `Image`
trong suốt, vì `Image` vẫn dựng mesh cho Canvas còn `RaycastBlocker` thì không.

## Cách dùng

Thêm component vào RectTransform phủ vùng cần chặn, thường là stretch kín
cha của nó.

- Không cần Sprite hay Material: component không tạo mesh, nên không có gì để vẽ.
- *Raycast Target* bật thì chặn, tắt thì không chặn. Mặc định là bật.
- Chỉ chặn những gì nằm **phía sau** nó trong hierarchy. Phần tử nằm phía trước
  vẫn nhận cú bấm trước.
