package com.dhbw.app.data.remote

import com.dhbw.app.domain.model.Document
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.forms.formData
import io.ktor.client.request.forms.submitFormWithBinaryData
import io.ktor.http.Headers
import io.ktor.http.HttpHeaders

class DocumentApi(private val client: HttpClient) {

    suspend fun uploadPhoto(
        filename: String,
        imageBytes: ByteArray,
    ): ApiResponse<Document> =
        client.submitFormWithBinaryData(
            url = "/api/photos/upload",
            formData = formData {
                append("file", imageBytes, Headers.build {
                    append(HttpHeaders.ContentDisposition, "filename=\"$filename\"")
                    append(HttpHeaders.ContentType, "image/jpeg")
                })
            },
        ).body()
}
