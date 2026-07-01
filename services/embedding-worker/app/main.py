from __future__ import annotations

from fastapi import FastAPI

from app.models.api import EmbedRequest, EmbedResponse, HealthResponse
from app.services.embedding_service import EmbeddingService


app = FastAPI(title="Stream Forge Embedding Worker", version="0.1.0")
embedding_service = EmbeddingService()


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        provider=embedding_service.default_provider,
        defaultModel=embedding_service.default_model,
        device=embedding_service.device,
    )


@app.post("/embed", response_model=EmbedResponse)
def embed(request: EmbedRequest) -> EmbedResponse:
    return embedding_service.embed(request)
