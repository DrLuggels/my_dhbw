# Frontend Dockerfile - Multi-stage build
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY src/Frontend/package.json ./

# Install dependencies
RUN npm install

# Copy source files
COPY src/Frontend/. .

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine

# Install wget for healthcheck
RUN apk add --no-cache wget

# Copy built files to nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx config
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
