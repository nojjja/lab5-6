FROM postgres:16-alpine

ENV POSTGRES_DB=tododb
ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=postgres

COPY init.sql /docker-entrypoint-initdb.d/init.sql
