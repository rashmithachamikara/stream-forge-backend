export type UploadSessionConfig = {
  baseUrl: string;
  accessToken?: string;
};

export type CreateUploadSessionRequest = {
  title: string;
  description?: string;
  totalSize: number;
  contentType?: string;
  categoryId?: string;
  visibility?: 'Public' | 'Private' | 'Internal';
  tagIds?: string[];
};

export type CreateUploadSessionResponse = {
  sessionId: string;
  videoId: string;
  expiresAt: string;
  videoTitle: string;
};

export type UploadTarget = {
  type: 'BackendEndpoint' | 'S3PresignedUrl';
  url: string;
  headers?: Record<string, string>;
  httpMethod?: string;
};

export type UploadPartRequest = {
  sessionId: string;
  partNumber: number;
  partSize: number;
  checksum: string;
  file: Blob;
};

export type CompleteUploadSessionRequest = {
  sessionId: string;
  fileName: string;
};

export type ChunkUploadResult = {
  partNumber: number;
  checksum: string;
};

export type UploadProgress = {
  uploadedBytes: number;
  totalBytes: number;
  uploadedParts: number;
  totalParts: number;
  currentPart: number;
  percent: number;
};

export type UploadFileRequest = {
  title: string;
  file: Blob;
  fileName: string;
  description?: string;
  contentType?: string;
  categoryId?: string;
  visibility?: 'Public' | 'Private' | 'Internal';
  tagIds?: string[];
  chunkSizeBytes?: number;
  onProgress?: (progress: UploadProgress) => void;
};

export type UploadFileResponse = {
  sessionId: string;
  videoId: string;
  fileName: string;
  uploadedParts: ChunkUploadResult[];
  completeResponse: unknown;
};

export class StreamForgeUploadClient {
  constructor(private readonly config: UploadSessionConfig) {}

  private resolveUrl(url: string): string {
    if (/^https?:\/\//i.test(url)) {
      return url;
    }

    return new URL(url, this.config.baseUrl).toString();
  }

  private buildHeaders(extraHeaders?: HeadersInit): HeadersInit {
    const headers: Record<string, string> = {
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

  async createSession(request: CreateUploadSessionRequest): Promise<CreateUploadSessionResponse> {
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

    return response.json() as Promise<CreateUploadSessionResponse>;
  }

  async getUploadTarget(sessionId: string, partNumber: number, partSize: number): Promise<UploadTarget> {
    const response = await fetch(
      `${this.config.baseUrl}/api/v1/uploads/sessions/${sessionId}/target?partNumber=${partNumber}&partSize=${partSize}`,
      {
        method: 'GET',
        headers: this.buildHeaders()
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to get upload target: ${response.status}`);
    }

    return response.json() as Promise<UploadTarget>;
  }

  async uploadPart(request: UploadPartRequest): Promise<void> {
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

  async uploadChunk(sessionId: string, partNumber: number, chunk: Blob): Promise<ChunkUploadResult> {
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

  async uploadFile(request: UploadFileRequest): Promise<UploadFileResponse> {
    const chunkSizeBytes = request.chunkSizeBytes ?? 5 * 1024 * 1024;

    const session = await this.createSession({
      title: request.title,
      description: request.description,
      totalSize: request.file.size,
      contentType: request.contentType ?? request.file.type ?? 'application/octet-stream',
      categoryId: request.categoryId,
      visibility: request.visibility,
      tagIds: request.tagIds
    });

    const uploadedParts: ChunkUploadResult[] = [];
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
      videoId: session.videoId,
      fileName: request.fileName,
      uploadedParts,
      completeResponse
    };
  }

  private async calculateChecksum(blob: Blob): Promise<string> {
    const bytes = await blob.arrayBuffer();
    const digest = await crypto.subtle.digest('SHA-256', bytes);
    return Array.from(new Uint8Array(digest))
      .map((value) => value.toString(16).padStart(2, '0'))
      .join('');
  }

  async completeSession(request: CompleteUploadSessionRequest): Promise<unknown> {
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
