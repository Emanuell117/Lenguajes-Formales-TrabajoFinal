# Proyecto Final - Lenguajes Formales

## Team Members
- Emanuell Torres  
- Alejandro Correa  
- Juan José Palacio  
- Samuel Granados  

## Tools Used
- Visual Studio Community 2022 (version 17.11.0)  
- .NET 8.0  
- C# 12  

## How to Run the Project Using Docker

If you want to run the project inside a Docker container, follow these steps:

### Prerequisites

- [Docker](https://www.docker.com/get-started) installed on your machine
- The project cloned locally

### Steps

1. Open a terminal and navigate to the root folder of the project (where the `Dockerfile` is located):

   ```bash
   cd path/to/project-folder
	```

2. Build the Docker image (replace `project-name` with your desired image name):

   ```bash
   docker build -t project-name .
   ```

3. Run a container based on the image you just built:

   ```bash
   docker run --rm -it project-name
   ```

   * The `--rm` flag removes the container after it stops.
   * The `-it` flags allow interactive terminal input/output.

4. Your application will start inside the container and run as if it were running locally.

## How to Run the Project on Windows

### Option 1: Using Visual Studio

1. Make sure you have installed:
   - [Visual Studio Community 2022](https://visualstudio.microsoft.com/) version 17.11 or higher
   - **.NET 8 SDK**
   - The **".NET Desktop Development"** workload

2. Clone this repository:
   ```bash
   git clone https://github.com/your-username/project-name.git
   cd project-name

3. Open the .sln file (if available) or open the folder with Visual Studio.

4. Set the main project as the startup project (if needed).

5. Press Ctrl + F5 to run without debugging, or F5 to run with debugging.

### Option 2: Using Command Line (Terminal or PowerShell)

1. Make sure you have installed:
   - [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

2. Clone the repository:
   ```bash
   git clone https://github.com/your-username/project-name.git
   cd project-name

3. Run the project:

   ```bash
   dotnet run
   ```

4. (Optional) Build the project:

   ```bash
   dotnet build
   ```
## How to Run the Project on Linux

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed
- Git installed
- A terminal (bash, zsh, etc.)

### Steps

1. Open a terminal and install the .NET 8 SDK if not already installed.  
   Follow the official instructions for your distribution:  
   [https://learn.microsoft.com/en-us/dotnet/core/install/linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)

2. Clone the repository:
   ```bash
   git clone https://github.com/your-username/project-name.git
   cd project-name
	```

3. Run the project:

   ```bash
   dotnet run
   ```

4. (Optional) Build the project:

   ```bash
   dotnet build
   ```
