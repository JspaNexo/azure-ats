# Stage 1: Build Frontend Assets
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies first for optimal caching
COPY frontend/package*.json ./
RUN npm ci

# Copy source code and build production bundle
COPY frontend/ ./
RUN npm run build

# Stage 2: Serve with Nginx
FROM nginx:alpine AS final
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

