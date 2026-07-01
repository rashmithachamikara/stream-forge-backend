from __future__ import annotations

from typing import Literal
from uuid import UUID

from pydantic import BaseModel, Field


EmbeddingProviderType = Literal["local-sentence-transformer"]


class EmbedItemRequest(BaseModel):
    chunkId: UUID
    text: str = Field(min_length=1)


class EmbedRequest(BaseModel):
    provider: EmbeddingProviderType = "local-sentence-transformer"
    model: str = Field(min_length=1)
    items: list[EmbedItemRequest] = Field(min_length=1)


class EmbedItemResponse(BaseModel):
    chunkId: UUID
    embedding: list[float]


class EmbedResponse(BaseModel):
    provider: EmbeddingProviderType
    model: str
    vectorSize: int = Field(ge=1)
    items: list[EmbedItemResponse]


class HealthResponse(BaseModel):
    status: str
    provider: EmbeddingProviderType
    defaultModel: str
    device: str
