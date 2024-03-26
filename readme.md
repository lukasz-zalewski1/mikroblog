# Mikroblog

## Description

Solution for the application contains two projects - **Quality Check** and **Videos Designer**

The application was developed in 2023 with simple goal in mind - to automate getting data from
the mikroblog website and later automatically create videos out of it.

First version of the application was a one monolith project in which certain difficulties were
not foreseen and in the end, it happened to be more of a manual video designer instead of fully
automated one.

New version is not targeted to be fully automated but rather a combination of more strict
quality check and fully capable video designer which requires manual work but with much greater
effect.

### Quality Check

The application checks ratings of specified ranges of discussions. The original post and comments
are reviewed to determine if a mikroblog discussion is worth forwarding to the Video Designer.

### Video Designer

The application is a Windows WPF application with WebView2 as its core. In the designer view
you can specify which entries and in what order should be included in the video. 
The application will combine screenshots and text-to-speech of every selected entry to create a video
in a format specifically meant to be displayed on TikTok.

## How to use

### Prerequisites
- Clone and compile the solution.
- In the solution folder you'll find a folder called workplace. 
This is where all discussion files, configs, and logs
are stored. You can move that folder wherever you want.
- Open your workplace folder and go to configs. You will need to specify certain values for QualityCheck and
TextToSpeech. 
	>Every line in any config looks like this **key=value**
	- Open **QualityConditions.txt** and fill minimum ratings of post and comment which will qualify			
    a discussion as worthy.
	- Open **TextToSpeechApi.txt** and fill in key and region values. The application uses Azure TextToSpeech
	services, so you'll need to have a running service. 
- In the directories of application executables you will find text file called *workplace.txt*, 
write path to your workplace directory there.
