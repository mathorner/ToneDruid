-- knowledge_documents stores the canonical reference sources we ingest.
CREATE TABLE IF NOT EXISTS knowledge_documents (
    id UUID PRIMARY KEY,
    source_name TEXT NOT NULL,
    blob_path TEXT NOT NULL,
    checksum TEXT NOT NULL,
    page_count INTEGER NOT NULL CHECK (page_count > 0),
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_knowledge_documents_source_blob
    ON knowledge_documents (source_name, blob_path);

CREATE UNIQUE INDEX IF NOT EXISTS idx_knowledge_documents_checksum
    ON knowledge_documents (checksum);

-- retrieval_requests logs each retrieval invocation and the document it surfaced.
CREATE TABLE IF NOT EXISTS retrieval_requests (
    id UUID PRIMARY KEY,
    query_text TEXT NOT NULL,
    best_match_document_id UUID NULL REFERENCES knowledge_documents(id),
    best_match_snippet TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_retrieval_requests_created_at
    ON retrieval_requests (created_at DESC);

CREATE INDEX IF NOT EXISTS idx_retrieval_requests_best_match
    ON retrieval_requests (best_match_document_id);

-- feedback stores thumbs up/down for retrieval outcomes.
CREATE TABLE IF NOT EXISTS feedback (
    id UUID PRIMARY KEY,
    retrieval_request_id UUID NOT NULL REFERENCES retrieval_requests(id) ON DELETE CASCADE,
    suggestion_id UUID NULL,
    rating TEXT NOT NULL CHECK (rating IN ('thumbs_up', 'thumbs_down')),
    note TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_feedback_request
    ON feedback (retrieval_request_id);

CREATE INDEX IF NOT EXISTS idx_feedback_rating
    ON feedback (rating);
