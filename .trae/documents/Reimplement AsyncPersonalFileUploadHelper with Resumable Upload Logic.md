I will modify `AsyncPersonalFileUploadHelper.cs` to implement the resumable upload logic based on the Java `ResumableUploadClient`.

**Key Changes:**

1.  **Protocol Implementation**:
    *   I will replace the legacy custom binary protocol in `AsyncPersonalFileUploadHelper.cs` with the `FA CE` frame protocol used in the Java client.
    *   **Frame Structure**: `Magic (2B) + Type (1B) + Flags (1B) + Length (4B) + Payload`.
    *   **Frame Types**: `RESUME_CHECK (0x05)`, `META_FRAME (0x01)`, `DATA_FRAME (0x02)`, `END_FRAME (0x03)`.

2.  **Workflow Update (`backgroundWorker_executePersonalUploadTransport_DoWork`)**:
    *   **Step 1: Connect**: Reuse `NetServiceContext.getSendFileSocket()` and `initSendFileOnlineTransportSocketAndConnect` to establish the TCP connection to the file server.
    *   **Step 2: MD5 Calculation**: Implement the "Fast MD5" strategy (File Path + Size + LastModified) to match the Java client's default behavior for quick resumable checks.
    *   **Step 3: Resume Check**:
        *   Send `RESUME_CHECK` frame with file metadata (JSON).
        *   Receive response to determine if it's a "new" upload or "resume" (and get the `uploadedSize`).
    *   **Step 4: Meta Data (if new)**:
        *   If status is "new", send `META_FRAME` with metadata.
        *   Wait for server readiness acknowledgment.
    *   **Step 5: Data Transmission**:
        *   Open `FileStream` and seek to the `uploadedSize` (offset).
        *   Read file in chunks (32KB as per Java demo).
        *   Send `DATA_FRAME` for each chunk.
        *   **Progress Update**: Update `DataGridViewProgressBarCell` and handle cancellation requests (`CancellationPending`).
    *   **Step 6: Completion**:
        *   Send `END_FRAME` when finished.
        *   Wait for final server acknowledgment.

3.  **Code Structure**:
    *   Delete the legacy `parseBytes`, `receiveHandler`, `executeUpload`, `waitingForFileIsNeedToOnlineTransport` methods as they are no longer needed.
    *   Add new helper methods: `SendFrame`, `ReceiveFrame`, `CalculateFastMD5`.
    *   Use `Newtonsoft.Json` (already available) for JSON serialization/deserialization.

**Verification**:
*   The logic will be self-contained within the helper class.
*   I will ensure exception handling and resource disposal (closing sockets/streams) are robust.
