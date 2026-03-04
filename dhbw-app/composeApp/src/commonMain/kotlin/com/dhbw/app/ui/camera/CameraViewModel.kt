package com.dhbw.app.ui.camera

import cafe.adriel.voyager.core.model.ScreenModel
import cafe.adriel.voyager.core.model.screenModelScope
import com.dhbw.app.data.remote.DocumentApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

enum class CameraPhase { IDLE, PREVIEW, UPLOADING, DONE, ERROR }

data class CameraState(
    val phase: CameraPhase = CameraPhase.IDLE,
    val photoBytes: ByteArray? = null,
    val documentTitle: String? = null,
    val errorMessage: String? = null,
)

class CameraViewModel(
    private val documentApi: DocumentApi,
) : ScreenModel {

    private val _state = MutableStateFlow(CameraState())
    val state: StateFlow<CameraState> = _state

    fun onPhotoCaptured(bytes: ByteArray) {
        _state.value = CameraState(phase = CameraPhase.PREVIEW, photoBytes = bytes)
    }

    fun retakePhoto() {
        _state.value = CameraState(phase = CameraPhase.IDLE)
    }

    fun uploadPhoto() {
        val bytes = _state.value.photoBytes ?: return
        screenModelScope.launch {
            _state.value = _state.value.copy(phase = CameraPhase.UPLOADING)
            try {
                val response = documentApi.uploadPhoto(
                    filename = "photo_${System.currentTimeMillis()}.jpg",
                    imageBytes = bytes,
                )
                if (response.success && response.data != null) {
                    _state.value = CameraState(
                        phase = CameraPhase.DONE,
                        documentTitle = response.data.title,
                    )
                } else {
                    _state.value = CameraState(
                        phase = CameraPhase.ERROR,
                        photoBytes = bytes,
                        errorMessage = response.message.ifBlank { "Upload fehlgeschlagen" },
                    )
                }
            } catch (e: Exception) {
                _state.value = CameraState(
                    phase = CameraPhase.ERROR,
                    photoBytes = bytes,
                    errorMessage = e.message ?: "Netzwerkfehler",
                )
            }
        }
    }

    fun reset() {
        _state.value = CameraState()
    }
}
