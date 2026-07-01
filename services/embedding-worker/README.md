# Embedding Worker

Internal worker for transcript embedding generation.

## Endpoints

- `GET /health`
- `POST /embed`

## Local Run

```powershell
cd services/embedding-worker
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python -m uvicorn app.main:app --host 0.0.0.0 --port 8091
```

## Environment Variables

- `EMBEDDING_DEFAULT_PROVIDER` default: `local-sentence-transformer`
- `EMBEDDING_DEFAULT_MODEL` default: `sentence-transformers/all-MiniLM-L6-v2`
- `EMBEDDING_DEVICE` default: `cpu`
