# Etapa 1 - Build do Angular
FROM node:24-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm ci

COPY . .

RUN npm run build

# Etapa 2 - Nginx
FROM nginx:stable-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/gestor-turnos-web/browser/ /usr/share/nginx/html/

EXPOSE 80
