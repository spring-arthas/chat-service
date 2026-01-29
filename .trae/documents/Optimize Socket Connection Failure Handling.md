I will modify `AsyncPersonalFileUploadHelper.cs` to handle the socket connection failure more gracefully as requested.

**Specific Changes:**

1.  **Locate Lines 126-129**:
    Currently, the code checks `connectResp.getResponse() != NetResponse.Response.CONNECTION_SUCCESS` and if true, it executes:
    ```csharp
    MessageBox.Show("连接服务器失败: " + connectResp.getResult(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
    ```
    However, simply calling `MessageBox.Show` in a `BackgroundWorker` thread (without `Invoke` if not handled by `MessageBox` implementation automatically, though `MessageBox.Show` is generally thread-safe in WinForms but blocks the current thread) is risky or might not appear centered on the parent. More importantly, after the message box closes, the code **continues execution** to step 4 (MD5 calculation), which will fail or behave unexpectedly since the socket is invalid.

2.  **Optimize the Logic**:
    *   **Invoke on UI Thread**: Use `Main_Form.main_Form.Invoke` to ensure the `MessageBox` is modal to the main window and runs on the UI thread.
    *   **Graceful Exit**: After the user clicks OK/Cancel, set `e.Cancel = true` (or throw a controlled exception) and `return` immediately to stop further processing.
    *   **Status Update**: Ensure the UI row status is updated to "Upload Failed" or "Connection Failed" before returning.

**Revised Code Snippet**:
```csharp
                if (connectResp.getResponse() != NetResponse.Response.CONNECTION_SUCCESS)
                {
                    // 1. Show Error Dialog on UI Thread
                    Main_Form.main_Form.Invoke(new MethodInvoker(delegate ()
                    {
                        MessageBox.Show("连接服务器失败: " + connectResp.getResult(), "系统提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    }));

                    // 2. Mark as Cancelled/Failed
                    e.Cancel = true;
                    this.dataGridViewRow.Cells[3].Value = "上传失败"; // Update status column

                    // 3. Stop Execution
                    return; 
                }
```

This ensures the user is notified, can close the dialog, and the background task terminates safely without crashing later.
