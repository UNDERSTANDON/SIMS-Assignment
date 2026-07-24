# SIMS Assignment

A .NET 9 ASP.NET Core web application for a Student Information Management System (SIMS). The project provides role-based workflows for students, lecturers/faculty, and administrators, with course enrollment, assignment handling, material uploads, and grade management.

## Overview

This application is built around a MVC architecture and uses CSV-backed persistence for the primary data model. It stores data under the `DataStorage` folder and seeds default demo accounts on first startup.

## Features

- User authentication and registration
- Role-based access for Admin, Faculty, and Student
- Student course browsing and enrollment
- Lecturer course management
- Material upload and download
- Assignment creation and submission workflow
- Grade viewing and updates
- CSV/JSON file persistence with a storage abstraction layer

## Tech Stack

- C#
- ASP.NET Core MVC
- .NET 9
- CSV/JSON-backed data storage
- xUnit for automated tests

## Project Structure

- `Controllers/` – MVC controllers for authentication, courses, grades, and dashboards
- `Models/` – domain models such as `User`, `Student`, `Course`, and related view models
- `Services/` – business logic for course, enrollment, and student operations
- `Storage/` – persistence, initialization, and file handling
- `DataStorage/` – runtime CSV/JSON data files
- `Tests/SIMS_Assignment.Tests/` – automated test project

## Prerequisites

Before running the project, make sure you have the following installed:

- .NET SDK 9.0
- A modern browser

## Run the Application

From the project root:

```bash
dotnet restore
dotnet run
```

The application will start with fallback local ports if the default ports are already in use. Typical local URLs are:

- `http://127.0.0.1:5126`
- `https://127.0.0.1:7235`

## Default Demo Accounts

The app seeds default users automatically on first startup when no users exist in `DataStorage/users.csv`.

- Admin
  - Username: `admin`
  - Password: `admin123`

- Faculty
  - Username: `giaovien`
  - Password: `faculty123`

- Student
  - Username: `sinhvien`
  - Password: `student123`

## Testing

Run the test suite from the repository root:

```bash
dotnet test
```

## Notes

- The application writes runtime data into the project `DataStorage` folder so data stays close to the source tree during local development.
- Uploaded materials are saved under `DataStorage/Materials`.
- The app includes request logging and runtime exception logging to the same `DataStorage` location for troubleshooting.

## License

This project is provided under the repository license.
