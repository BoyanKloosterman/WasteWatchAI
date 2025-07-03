const fs = require('fs');
const path = require('path');

class AuthService {
    constructor() {
        this.jwtToken = null;
        this.tokenExpiryTime = null;
        this.baseUrl = process.env.API_URL || 'http://localhost:8080';
        this.loginUrl = `${this.baseUrl}/account/login`;
        this.loginCredentials = {
            email: "serviceaccount@example.com",
            password: this.readPassword()
        };
    }

    readPassword() {
        try {
            // Zoek password.txt in de FastAPI folder
            const passwordPath = path.join(__dirname, '../../../../WasteWatchAIFastApi/password.txt');
            
            if (fs.existsSync(passwordPath)) {
                const content = fs.readFileSync(passwordPath, 'utf8').trim();
                
                // Parse password=value format
                if (content.includes('=')) {
                    const password = content.split('=')[1].trim();
                    console.log('🔑 Password loaded from FastAPI folder');
                    return password;
                }
                
                // Return as is if no = found
                return content;
            } else {
                console.log('⚠️  password.txt not found in FastAPI folder, using default');
            }
        } catch (error) {
            console.error('⚠️  Error reading password file:', error);
        }
    }

    async login() {
        try {
            console.log('🔐 Attempting login to get JWT token...');
            
            const fetch = (await import('node-fetch')).default;
            
            const response = await fetch(this.loginUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(this.loginCredentials),
                timeout: 15000
            });

            if (response.status === 200) {
                const data = await response.json();
                this.jwtToken = data.accessToken;
                
                // Set token expiry time (assume 1 hour if not specified)
                this.tokenExpiryTime = Date.now() + (60 * 60 * 1000);
                
                console.log('✅ JWT token obtained successfully');
                return this.jwtToken;
            } else {
                const errorText = await response.text();
                console.error(`❌ Login failed: ${response.status} ${response.statusText} - ${errorText}`);
                throw new Error(`Login failed: ${response.status}`);
            }
        } catch (error) {
            console.error('❌ Error during login:', error.message);
            throw error;
        }
    }

    async getValidToken() {
        // Check if token exists and is not expired
        if (!this.jwtToken || (this.tokenExpiryTime && Date.now() >= this.tokenExpiryTime)) {
            console.log('🔄 Token expired or missing, logging in...');
            await this.login();
        }
        
        return this.jwtToken;
    }

    async getAuthHeaders() {
        const token = await this.getValidToken();
        return {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        };
    }

    // Retry logic for API calls with authentication
    async makeAuthenticatedRequest(url, options = {}, maxRetries = 3) {
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                const headers = await this.getAuthHeaders();
                
                const fetch = (await import('node-fetch')).default;
                const response = await fetch(url, {
                    ...options,
                    headers: {
                        ...headers,
                        ...options.headers
                    }
                });

                if (response.status === 401) {
                    // Unauthorized - token might be expired, try to refresh
                    console.log('🔄 Token expired, refreshing...');
                    this.jwtToken = null;
                    this.tokenExpiryTime = null;
                    
                    if (attempt === maxRetries) {
                        throw new Error('Authentication failed after token refresh');
                    }
                    continue;
                }

                return response;
            } catch (error) {
                if (attempt === maxRetries) {
                    throw error;
                }
                
                console.log(`⚠️  Request attempt ${attempt} failed, retrying...`);
                await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
            }
        }
    }
}

module.exports = AuthService;
