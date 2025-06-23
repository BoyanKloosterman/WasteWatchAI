# Mock API for Trash Items

This project is a mock API that simulates the behavior of a backend service for managing trash items. It now includes **auto-generation** of realistic trash data.

## 🆕 New Features

- **Auto-generation**: Automatically adds 1-3 new realistic items every 5 minutes
- **Realistic data**: 
  - Real Breda coordinates (Grote Markt, Station, etc.)
  - Realistic time patterns (more activity during daytime)
  - Weighted waste types (Plastic = 45%, Paper = 25%, etc.)
- **Query filtering**: Support for filtering by type, time period, etc.
- **Statistics endpoint**: Get data insights

## Project Structure

```
mock-api
├── src
│   ├── server.js              # Entry point with auto-generation
│   ├── routes
│   │   └── trashItems.js      # Enhanced routes with filtering
│   ├── data
│   │   └── dummyTrashItems.json # Auto-updating mock data
│   ├── utils                  # NEW!
│   │   └── dataGenerator.js   # Smart data generation
│   └── middleware
│       └── cors.js            # CORS middleware configuration
├── package.json               # Updated with new scripts
└── README.md                  # Enhanced documentation
```

## Getting Started

### Prerequisites

- Node.js (version 14 or higher)
- npm (Node Package Manager)

### Installation

1. Navigate to the mock-api directory:
   ```
   cd mock-api
   ```

2. Install the dependencies:
   ```
   npm install
   ```

### Running the Mock API

To start the mock API server:

```bash
npm start
# or for development with auto-reload:
npm run dev
```

The server will start on `http://localhost:8080` and:
- Auto-generate realistic data on first run
- Add new items every 5 minutes automatically
- Show generation logs in console

## API Endpoints

### Main Endpoint
- **GET** `/api/TrashItems/dummy`: Returns array of dummy trash items

### New Endpoints
- **GET** `/api/TrashItems/dummy?limit=10`: Limit results
- **GET** `/api/TrashItems/dummy?litterType=Plastic`: Filter by waste type
- **GET** `/api/TrashItems/dummy?days=7`: Last 7 days only
- **POST** `/api/TrashItems/dummy/generate`: Manually generate new items
- **GET** `/api/TrashItems/dummy/stats`: Get data statistics

### Manual Commands

```bash
# Generate 10 new items manually
npm run generate

# Start with development auto-reload
npm run dev
```

## Example Usage

```javascript
// Get all items
const response = await fetch('http://localhost:8080/api/TrashItems/dummy');

// Get last 10 plastic items from last week
const filtered = await fetch('http://localhost:8080/api/TrashItems/dummy?limit=10&litterType=Plastic&days=7');

// Generate 5 new items manually
await fetch('http://localhost:8080/api/TrashItems/dummy/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ count: 5 })
});
```

## Integration with WasteWatch Frontend

Your frontend will now receive continuously updated realistic data! The existing endpoint remains the same, but now provides:
- ✅ Realistic Breda locations
- ✅ Proper time distribution 
- ✅ Weighted waste type distribution
- ✅ Automatic updates every 5 minutes

## License

This project is licensed under the MIT License.