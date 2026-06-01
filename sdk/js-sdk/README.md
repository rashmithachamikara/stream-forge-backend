# StreamForge JS SDK

This package contains the client upload helper for StreamForge video uploads.

## Development

```bash
npm install
npm run build
```

## Usage

```ts
import { StreamForgeUploadClient } from './dist/index.js';

const client = new StreamForgeUploadClient({
  baseUrl: 'https://localhost:5186',
  accessToken: 'your-jwt-token'
});
```
