export class StreamForgeUploadClient {
    config;
    constructor(config) {
        this.config = config;
    }
    resolveUrl(url) {
        if (/^https?:\/\//i.test(url)) {
            return url;
        }
        return new URL(url, this.config.baseUrl).toString();
    }
    buildHeaders(extraHeaders) {
        const headers = {
            Accept: 'application/json'
        };
        if (this.config.accessToken) {
            headers.Authorization = `Bearer ${this.config.accessToken}`;
        }
        return {
            ...headers,
            ...(extraHeaders ?? {})
        };
    }
    async createSession(request) {
        const response = await fetch(`${this.config.baseUrl}/api/v1/uploads/sessions`, {
            method: 'POST',
            headers: this.buildHeaders({
                'Content-Type': 'application/json'
            }),
            body: JSON.stringify(request)
        });
        if (!response.ok) {
            throw new Error(`Failed to create upload session: ${response.status}`);
        }
        return response.json();
    }
    async getUploadTarget(sessionId, partNumber, partSize) {
        const response = await fetch(`${this.config.baseUrl}/api/v1/uploads/sessions/${sessionId}/target?partNumber=${partNumber}&partSize=${partSize}`, {
            method: 'GET',
            headers: this.buildHeaders()
        });
        if (!response.ok) {
            throw new Error(`Failed to get upload target: ${response.status}`);
        }
        return response.json();
    }
    async uploadPart(request) {
        const target = await this.getUploadTarget(request.sessionId, request.partNumber, request.partSize);
        if (target.type === 'S3PresignedUrl') {
            const uploadResponse = await fetch(this.resolveUrl(target.url), {
                method: target.httpMethod ?? 'PUT',
                headers: target.headers,
                body: request.file
            });
            if (!uploadResponse.ok) {
                throw new Error(`Failed to upload part ${request.partNumber}`);
            }
            return;
        }
        const formData = new FormData();
        formData.append('file', request.file, `part_${request.partNumber}.bin`);
        formData.append('checksum', request.checksum);
        const uploadResponse = await fetch(this.resolveUrl(target.url), {
            method: target.httpMethod ?? 'POST',
            headers: this.buildHeaders(),
            body: formData
        });
        if (!uploadResponse.ok) {
            throw new Error(`Failed to upload part ${request.partNumber}`);
        }
    }
    async uploadChunk(sessionId, partNumber, chunk) {
        const checksum = await this.calculateChecksum(chunk);
        const target = await this.getUploadTarget(sessionId, partNumber, chunk.size);
        if (target.type === 'S3PresignedUrl') {
            const uploadResponse = await fetch(this.resolveUrl(target.url), {
                method: target.httpMethod ?? 'PUT',
                headers: target.headers,
                body: chunk
            });
            if (!uploadResponse.ok) {
                throw new Error(`Failed to upload chunk ${partNumber}`);
            }
            return { partNumber, checksum };
        }
        const formData = new FormData();
        formData.append('file', chunk, `part_${partNumber}.bin`);
        formData.append('checksum', checksum);
        const uploadResponse = await fetch(this.resolveUrl(target.url), {
            method: target.httpMethod ?? 'POST',
            headers: this.buildHeaders(),
            body: formData
        });
        if (!uploadResponse.ok) {
            throw new Error(`Failed to upload chunk ${partNumber}`);
        }
        return { partNumber, checksum };
    }
    async uploadFile(request) {
        const chunkSizeBytes = request.chunkSizeBytes ?? 5 * 1024 * 1024;
        const session = await this.createSession({
            title: request.title,
            description: request.description,
            totalSize: request.file.size,
            contentType: request.contentType ?? request.file.type ?? 'application/octet-stream',
            categoryId: request.categoryId
        });
        const uploadedParts = [];
        const totalParts = Math.ceil(request.file.size / chunkSizeBytes);
        let uploadedBytes = 0;
        let partNumber = 1;
        for (let offset = 0; offset < request.file.size; offset += chunkSizeBytes) {
            const chunk = request.file.slice(offset, offset + chunkSizeBytes);
            const uploadedPart = await this.uploadChunk(session.sessionId, partNumber, chunk);
            uploadedParts.push(uploadedPart);
            uploadedBytes += chunk.size;
            request.onProgress?.({
                uploadedBytes,
                totalBytes: request.file.size,
                uploadedParts: uploadedParts.length,
                totalParts,
                currentPart: partNumber,
                percent: request.file.size > 0 ? Math.round((uploadedBytes / request.file.size) * 100) : 100
            });
            partNumber += 1;
        }
        const completeResponse = await this.completeSession({
            sessionId: session.sessionId,
            fileName: request.fileName
        });
        return {
            sessionId: session.sessionId,
            fileName: request.fileName,
            uploadedParts,
            completeResponse
        };
    }
    async calculateChecksum(blob) {
        const bytes = await blob.arrayBuffer();
        const digest = await crypto.subtle.digest('SHA-256', bytes);
        return Array.from(new Uint8Array(digest))
            .map((value) => value.toString(16).padStart(2, '0'))
            .join('');
    }
    async completeSession(request) {
        const response = await fetch(`${this.config.baseUrl}/api/v1/uploads/sessions/${request.sessionId}/complete`, {
            method: 'POST',
            headers: this.buildHeaders({
                'Content-Type': 'application/json'
            }),
            body: JSON.stringify(request)
        });
        if (!response.ok) {
            throw new Error(`Failed to complete session: ${response.status}`);
        }
        return response.json();
    }
}
