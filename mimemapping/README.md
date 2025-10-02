# MimeMapping Demo

This interactive Ivy application demonstrates the capabilities of the [MimeMapping](https://github.com/zone117x/MimeMapping) library.

## Features Demonstrated

### 1. File Extension to MIME Type Detection
- Enter any file name, extension, or full path
- Get instant MIME type detection
- Handles unknown file types gracefully (returns pplication/octet-stream)
- Interactive examples for common file types

### 2. Browse Available MIME Types
- View all 1000+ supported MIME types
- Search and filter by extension or MIME type
- Tabular display for easy browsing
- Real-time search functionality

### 3. Reverse Lookup (MIME Type to Extensions)
- Enter a MIME type to find associated file extensions
- Visual display of all matching extensions
- Examples for common MIME types
- Handles unknown MIME types gracefully

## Key MimeMapping Features Showcased

- **Core Functionality**: MimeUtility.GetMimeMapping(file) - detects MIME type from file extension
- **Reverse Lookup**: MimeUtility.GetExtensions(mimeType) - finds extensions for a MIME type
- **Type Browsing**: MimeUtility.TypeMap - access to all 1000+ MIME type mappings
- **Unknown Handling**: Graceful fallback to pplication/octet-stream for unknown types
- **Performance**: Lazy-loaded dictionaries for optimal performance

## Running the Demo

1. Ensure you have .NET 9.0 installed
2. Run the application: dotnet run
3. The demo will open in your default browser
4. Use the tabs to explore different MimeMapping features

## Integration with Ivy Framework

This demo showcases how to integrate third-party libraries with Ivy:
- Clean separation of concerns with dedicated demo sections
- Reactive state management using Ivy's UseState hooks
- Interactive UI components with real-time updates
- Professional layout with cards, buttons, and tables
- Comprehensive error handling and user feedback

## Code Quality Features

- **Comprehensive Comments**: Explains MimeMapping usage and Ivy integration
- **Clean Architecture**: Modular design with separate methods for each demo section
- **User Experience**: Interactive examples, search functionality, and visual feedback
- **Error Handling**: Graceful handling of edge cases and unknown inputs
- **Performance**: Efficient filtering and lazy loading of data
