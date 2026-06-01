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
};
export type CreateUploadSessionResponse = {
    sessionId: string;
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
    chunkSizeBytes?: number;
    onProgress?: (progress: UploadProgress) => void;
};
export type UploadFileResponse = {
    sessionId: string;
    fileName: string;
    uploadedParts: ChunkUploadResult[];
    completeResponse: unknown;
};
export declare class StreamForgeUploadClient {
    private readonly config;
    constructor(config: UploadSessionConfig);
    private resolveUrl;
    private buildHeaders;
    createSession(request: CreateUploadSessionRequest): Promise<CreateUploadSessionResponse>;
    getUploadTarget(sessionId: string, partNumber: number, partSize: number): Promise<UploadTarget>;
    uploadPart(request: UploadPartRequest): Promise<void>;
    uploadChunk(sessionId: string, partNumber: number, chunk: Blob): Promise<ChunkUploadResult>;
    uploadFile(request: UploadFileRequest): Promise<UploadFileResponse>;
    private calculateChecksum;
    completeSession(request: CompleteUploadSessionRequest): Promise<unknown>;
}
