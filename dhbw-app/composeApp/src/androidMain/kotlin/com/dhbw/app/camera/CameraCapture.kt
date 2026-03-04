package com.dhbw.app.camera

import android.content.Context
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.FileProvider
import java.io.File

@Composable
fun rememberCameraLauncher(
    onPhotoCaptured: (ByteArray) -> Unit,
): () -> Unit {
    val context = LocalContext.current
    val photoUri = remember { createTempImageUri(context) }

    val launcher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.TakePicture(),
    ) { success ->
        if (success) {
            val bytes = context.contentResolver.openInputStream(photoUri)?.readBytes()
            if (bytes != null) {
                onPhotoCaptured(bytes)
            }
        }
    }

    return { launcher.launch(photoUri) }
}

private fun createTempImageUri(context: Context): Uri {
    val tempFile = File.createTempFile("photo_", ".jpg", context.cacheDir).apply {
        createNewFile()
        deleteOnExit()
    }
    return FileProvider.getUriForFile(
        context,
        "${context.packageName}.fileprovider",
        tempFile,
    )
}
