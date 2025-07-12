pipeline {
    agent any
    
    environment {
        // Variables para SonarQube
        SONAR_PROJECT_KEY = 'mi-proyecto-csharp'
        SONAR_PROJECT_NAME = 'TitaFinal'
        SONAR_HOST_URL = 'http://localhost:9000'
        // Path para .NET (asumiendo instalación estándar)
        PATH = "${env.PATH};C:\\Program Files\\dotnet"
    }
    
    stages {
        stage('Checkout') {
            steps {
                echo 'Clonando repositorio...'
                checkout scm
            }
        }
        
        stage('Restore Dependencies') {
            steps {
                echo 'Restaurando dependencias de .NET...'
                bat 'dotnet restore'
            }
        }
        
        stage('SonarQube Analysis Start') {
            steps {
                echo 'Iniciando análisis de SonarQube...'
                withSonarQubeEnv('SonarQube') {
                    script {
                        def scannerHome = tool 'SonarQube Scanner'
                        bat """
                            "${scannerHome}\\bin\\sonar-scanner.bat" ^
                                -Dsonar.projectKey=${SONAR_PROJECT_KEY} ^
                                -Dsonar.projectName="${SONAR_PROJECT_NAME}" ^
                                -Dsonar.sources=. ^
                                -Dsonar.host.url=${SONAR_HOST_URL} ^
                                -Dsonar.exclusions=**/bin/**,**/obj/**,**/*.dll,**/packages/** ^
                                -Dsonar.cs.opencover.reportsPaths=**/coverage.opencover.xml
                        """
                    }
                }
            }
        }
        
        stage('Build') {
            steps {
                echo 'Compilando proyecto...'
                bat 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test') {
            steps {
                echo 'Ejecutando pruebas...'
                bat 'dotnet test --no-restore --no-build --configuration Release --logger trx --collect:"XPlat Code Coverage"'
            }
        }
        
        stage('SonarQube Analysis End') {
            steps {
                echo 'Análisis de SonarQube completado en stage anterior'
                // El scanner general de SonarQube no requiere comando "end"
            }
        }
        
        stage('Quality Gate') {
            steps {
                echo 'Esperando resultado del Quality Gate...'
                timeout(time: 5, unit: 'MINUTES') {
                    waitForQualityGate abortPipeline: true
                }
            }
        }
    }
    
    post {
        always {
            echo 'Pipeline completado'
            // Limpiar archivos temporales si es necesario
        }
        success {
            echo 'Pipeline ejecutado exitosamente!'
        }
        failure {
            echo 'Pipeline falló. Revisar logs.'
        }
    }
}
