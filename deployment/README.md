# PBIHoster Deployment Guide

This folder contains the necessary files to deploy the PBIHoster application using Docker.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed on your machine.
- [Docker Compose](https://docs.docker.com/compose/install/) (usually included with Docker Desktop).

## Quick Start

1.  Download the `docker-compose.yml` file from this folder to your local machine.
2.  Open a terminal in the directory where you saved the file.
3.  Run the following command to start the application:

    ```bash
    docker compose up -d
    ```

4.  The application will be available at [http://localhost:8080](http://localhost:8080).

## Updating the Application

To update to the latest version of the application:

1.  Pull the latest image:

    ```bash
    docker compose pull
    ```

2.  Restart the container:

    ```bash
    docker compose up -d
    ```

## Troubleshooting

If you encounter any issues, please check the [Issues](https://github.com/Aenas11/PBIHoster/issues) page on our GitHub repository.

