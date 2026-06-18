from __future__ import annotations

from threading import Lock

from app.models.jobs import WorkerJob


class JobStore:
    def __init__(self) -> None:
        self._lock = Lock()
        self._jobs: dict[str, WorkerJob] = {}

    def add(self, job: WorkerJob) -> WorkerJob:
        with self._lock:
            self._jobs[job.id] = job
        return job

    def get(self, job_id: str) -> WorkerJob | None:
        with self._lock:
            return self._jobs.get(job_id)
