# OPOSProj

## **Project Overview**

**OPOSProj** is an advanced project that demonstrates the implementation of a custom file system using the Dokan library, a sophisticated task scheduler for parallel processing, and a feature-rich GUI for managing tasks and image processing.

## **Projects**

1. **OposFS:** Implements a custom file system using the Dokan library.
2. **OposMMOP:** Contains the logic for image sharpening and processing.
3. **OposScheduler:** Implements a custom task scheduler for managing task execution.
4. **OposGui:** A WPF application that provides a GUI for managing tasks and image processing.
5. **TestProject1:** Contains unit tests for validating the functionality of other projects.

## Tech Stack

- **.NET 6:** The core framework for building and running the application.
- **C#:** The programming language used for the development.
- **Dokan:** A library for creating custom file systems in Windows.
- **WPF (Windows Presentation Foundation):** For creating the graphical user interface.
- **XUnit:** For unit testing.

### Screenshot

![MainWindow Screenshot](https://github.com/andjeladragosavljevic/OPOSProj/blob/master/OPOSGui.png?raw=true)

### Usage

### Setup

1. **Clone the Repository**
    ```sh
    git clone https://github.com/andjeladragosavljevic/OPOSProj
    cd OPOSProj
    ```

2. **Install Dependencies**
    Ensure you have .NET 6 SDK installed. Restore NuGet packages using:
    ```sh
    dotnet restore
    ```

3. **Build the Solution**
    ```sh
    dotnet build
    ```

### Run the Application

1. **OposGui**

    The GUI application allows you to manage tasks and process images.

    ```sh
    dotnet run --project OposGui
    ```

2. **Image Processing with Dokan Library**

    The Dokan library is used to create a custom file system for processing images. The images are read from an input folder, processed, and then written to an output folder.

    - **Create the Input and Output Folders**: Ensure that the folders `input` and `output` exist in the directory where the application is run.

### Example Usage

#### Add Task

- **Function Task**
    - **Total Execution Time**: `1000`
    - **Deadline**: `5000`
    - **Max Degree of Parallelism**: `2`

    ```sh
    Create task with the above parameters using the GUI.
    ```

- **Sharpen Image Task**
    - **Select Images**: Choose images from the file dialog.
    - **Max Degree of Parallelism**: `2`
    - **Total Execution Time**: `1000`
    - **Deadline**: `5000`

    ```sh
    Create sharpen task with the above parameters using the GUI.
    ```

#### Image Processing

- **Process Images**
    - Select images from the `input` folder.
    - Process the images using the `SharpImageButton`.

    ```sh
    Click on "Search images" to select images from the input folder.
    Click on "Process data" to start processing.
    ```

