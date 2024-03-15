# Mikroblog (Work in Progress)

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

## Quality Check

The application checks ratings of the original post and comments to determine if a mikroblog 
discussion is worthy forwarding to the Video Designer.

## Video Designer

The application is a Windows WPF application with WebView2 as its core. In the designer view
you can specify which entries and in what order should be included in the video. 
The application will combine screenshots and text-to-speech of every entry to create a video
in a format specifically meant to be displayed on TikTok.
