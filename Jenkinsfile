pipeline {
    agent any
    
    environment {
        SONAR_PROJECT_KEY = 'TitaFinal-Clean'
        SONAR_PROJECT_NAME = 'TitaFinal Clean'
        SONAR_HOST_URL = 'http://localhost:9000'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Restore') {
            steps {
                bat 'dotnet restore'
            }
        }
        
        stage('SonarQube Begin') {
            steps {
                withSonarQubeEnv('SonarQube') {
                    bat 'C:\\Users\\HOME\\.dotnet\\tools\\dotnet-sonarscanner.exe begin /k:"%SONAR_PROJECT_KEY%" /n:"%SONAR_PROJECT_NAME%" /d:sonar.host.url="%SONAR_HOST_URL%"'
                }
            }
        }
        
        stage('Build') {
            steps {
                bat 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test') {
            steps {
                bat 'dotnet test --no-restore --no-build --configuration Release'
            }
        }
        
        stage('SonarQube End') {
            steps {
                withSonarQubeEnv('SonarQube') {
                    bat 'C:\\Users\\HOME\\.dotnet\\tools\\dotnet-sonarscanner.exe end'
                }
            }
        }
        
        stage('Quality Gate') {
            steps {
                timeout(time: 5, unit: 'MINUTES') {
                    waitForQualityGate abortPipeline: true
                }
            }
        }
    }
}
