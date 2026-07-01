from __future__ import annotations

import os
from threading import Lock

from sentence_transformers import SentenceTransformer

from app.models.api import EmbedRequest, EmbedResponse, EmbedItemResponse


class EmbeddingService:
    def __init__(self) -> None:
        self._default_provider = os.getenv("EMBEDDING_DEFAULT_PROVIDER", "local-sentence-transformer")
        self._default_model = os.getenv("EMBEDDING_DEFAULT_MODEL", "sentence-transformers/all-MiniLM-L6-v2")
        self._device = os.getenv("EMBEDDING_DEVICE", "cpu")
        self._models: dict[str, SentenceTransformer] = {}
        self._lock = Lock()

    @property
    def default_provider(self) -> str:
        return self._default_provider

    @property
    def default_model(self) -> str:
        return self._default_model

    @property
    def device(self) -> str:
        return self._device

    def embed(self, request: EmbedRequest) -> EmbedResponse:
        model = self._get_model(request.model)
        texts = [item.text.strip() for item in request.items]
        embeddings = model.encode(texts, normalize_embeddings=True)

        vector_size = len(embeddings[0]) if len(embeddings) > 0 else 0
        items = [
            EmbedItemResponse(
                chunkId=item.chunkId,
                embedding=[float(value) for value in embedding],
            )
            for item, embedding in zip(request.items, embeddings, strict=True)
        ]

        return EmbedResponse(
            provider=request.provider,
            model=request.model,
            vectorSize=vector_size,
            items=items,
        )

    def _get_model(self, model_name: str) -> SentenceTransformer:
        with self._lock:
            if model_name not in self._models:
                self._models[model_name] = SentenceTransformer(model_name, device=self._device)

            return self._models[model_name]
