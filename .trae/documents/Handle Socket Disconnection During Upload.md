I will modify `AsyncPersonalFileUploadHelper.cs` to handle socket disconnection during the upload process.

**Changes:**

1.  **Add Try-Catch in SendFrame**:
    *   Currently, `SendFrame` calls `fileSendSocket.Send` without explicit error handling.
    *   I will wrap the socket send operation in a `try-catch` block.
    *   If a `SocketException` occurs, I will throw a custom `Exception` with a descriptive message (e.g., "Network disconnected during upload"). This will propagate to the main `DoWork` loop's `catch` block.

2.  **Enhance DoWork Loop**:
    *   The existing `try-catch` in `backgroundWorker_executePersonalUploadTransport_DoWork` already handles general exceptions.
    *   By ensuring `SendFrame` (and `ReceiveFrame`) throws on network failure, the existing `catch` block will correctly log "Upload Exception: [Message]" and update the UI status to "Upload Failed".
    *   No major structural change is needed in `DoWork` itself, just ensuring the low-level network methods propagate errors correctly.

**Specific Implementation**:
*   Modify `SendFrame`: Wrap `socket.Send` in `try-catch`. On `SocketException`, throw `new Exception("网络连接中断")`.
*   Modify `ReceiveFrame`: Wrap `socket.Receive` in `try-catch`. On `SocketException` or if bytes read is 0 (connection closed by peer), throw `new Exception("网络连接中断")`.

This ensures that any network interruption during the file chunk transmission is caught, the loop is broken, resources are cleaned up in `finally`, and the user is notified via the log and status update.
