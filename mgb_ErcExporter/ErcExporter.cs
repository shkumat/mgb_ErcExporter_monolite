// Версия 3.02 от 23.05.2019г. Создание файлов для ЕРЦ
//
// v3 - добавлена поддержка измененной структуры А-файла с учетом IBAN. 
//
using MyTypes;

class ErcExporter
{

	static	int		BFileNum	;
	static  int             SeanceNum	;
	static	CErcConfig	ErcConfig	;
	static	CConnection	Connection1	;
	static	CConnection	Connection2	;
	static	CScrooge2Config	Scrooge2Config	;
	static	CMd5Hash	Md5		=	new CMd5Hash();
	static	CSepAWriter	BFile		=	new CSepAWriter();
	static	readonly string Now_Date_Str	=	CCommon.DtoC(CCommon.Today()).Substring(2, 6);
	static	readonly string Now_Time_Str	=	CCommon.Hour(CCommon.Now()).ToString("00") + CCommon.Minute(CCommon.Now()).ToString("00");

	public	static	bool	WriteBFiles( int DayDate, string DestinationFolder )
	{
		if (DestinationFolder == null)
			return false;

		int FileId;
		CRecordSet Data_Line = new CRecordSet(Connection1);
		CRecordSet Data_Header = new CRecordSet(Connection2);
		string	DebitAcc , CreditAcc
		,	CmdText		=	"exec pMega_OpenGate_Export;2 @TaskCode = 'ErcGate' "
					+	", @Date="	+	DayDate.ToString()
					+	", @SeansNum="	+	BFileNum.ToString()
					+	", @FileName=''"	;
		if (Data_Header.Open(CmdText))
		{
			while (Data_Header.Read())
			{
				FileId = CCommon.CInt32(Data_Header["FileId"]);

				if (Data_Line.Open("exec dbo.pMega_OpenGate_Export;3 @FileId=" + FileId.ToString()))
				{

					if (BFile.Create( DestinationFolder + "\\" + Data_Header["FileName"].Trim(), CAbc.CHARSET_DOS)) {

						BFile.Head[CSepAFileInfo.H_EMPTYSTR]	=	"";				// char[100]  // Пеpвые 100 - пpобелы
						BFile.Head[CSepAFileInfo.H_CRLF1]	=	CAbc.CRLF;			// char[  2]; // Символ концец строки
						BFile.Head[CSepAFileInfo.H_FILENAME]	=	Data_Header["FileName"].Trim();	// char[ 12]; // Наименование  файла
						BFile.Head[CSepAFileInfo.H_DATE]	=	Now_Date_Str;			// char[  6]; // Дата создания файла
						BFile.Head[CSepAFileInfo.H_TIME]	=	Now_Time_Str;			// char[  4]; // Дата создания файла
						BFile.Head[CSepAFileInfo.H_STRCOUNT]	=	Data_Header["TotalLines"].Trim();// char[  6]; // Количество ИС в файле
						BFile.Head[CSepAFileInfo.H_TOTALDEBET]	=	"0";				// char[ 16]; // Сумма дебета по файлу
						BFile.Head[CSepAFileInfo.H_TOTALCREDIT]	=	Data_Header["TotalCents"].Trim();// char[ 16]; // Сумма кpедита по файлу
						BFile.Head[CSepAFileInfo.H_DES]		=	Md5.GetHash(BFile.GetHeader().Substring(102, 60));	// char[ 64]; // ЕЦП
						BFile.Head[CSepAFileInfo.H_DES_ID]	=	"UIAB00";			// char[  6]; // ID ключа ЕЦП
						BFile.Head[CSepAFileInfo.H_DES_OF_HEADER]=	"";				// char[ 64]; // ЕЦП заголовка
						BFile.Head[CSepAFileInfo.H_CRLF2]	=	CAbc.CRLF;			// char[  2]; // Символ конец строки

						if ( ! BFile.WriteHeader())
						{
							Data_Header.Close();
							Data_Line.Close();
							BFile.Close();
							return false;
						}

						while (Data_Line.Read())
						{
							DebitAcc=Data_Line["DebitAcc"].Trim();
							CreditAcc=Data_Line["CreditAcc"].Trim();
							BFile.Line[CSepAFileInfo.L_DEBITMFO]	=	Data_Line["DebitMfo"].Trim();				// char[  9]; // Дебет-МФО
							BFile.Line[CSepAFileInfo.L_DEBITACC]	=	DebitAcc;						// char[ 14]; // Дебет-счет
							BFile.Line[CSepAFileInfo.L_DEBITACC_EXT]=	( DebitAcc.Length>14 ? DebitAcc : "" );			// char[ 20]; // Расширенный Дебет-счет
							BFile.Line[CSepAFileInfo.L_DEBITIBAN]	=	Data_Line["DebitIBAN"].Trim();				// char[ 34]; // Дебет-IBAN
							BFile.Line[CSepAFileInfo.L_OKPO1]	=	Data_Line["DebitState"].Trim();				// char[ 14]; // Идент.код клиента А
							BFile.Line[CSepAFileInfo.L_DEBITNAME]	=	Data_Line["DebitName"].Trim().Replace("?", "i");	// char[ 38]; // Наименование дебет-счета
							BFile.Line[CSepAFileInfo.L_CREDITMFO]	=	Data_Line["CreditMfo"].Trim();				// char[  9]; // Кредит-МФО
							BFile.Line[CSepAFileInfo.L_CREDITACC]	=	CreditAcc;						// char[ 14]; // Кредит счет
							BFile.Line[CSepAFileInfo.L_CREDITACC_EXT]=	( CreditAcc.Length>14 ? CreditAcc : "" );		// char[ 20]; // Расширенный Кредит счет
							BFile.Line[CSepAFileInfo.L_OKPO2]	=	Data_Line["CreditState"].Trim();			// char[ 14]; // Идент.код клиента Б
							BFile.Line[CSepAFileInfo.L_CREDITIBAN]	=	Data_Line["CreditIBAN"].Trim();				// char[ 34]; // Кредит-IBAN
							BFile.Line[CSepAFileInfo.L_CREDITNAME]	=	Data_Line["CreditName"].Trim().Replace("?", "i");	// char[ 38]; // Наименование кредит-счета
							BFile.Line[CSepAFileInfo.L_FLAG]	=	"1";							// char[  1]; // Флаг `дебет/кредит`
							BFile.Line[CSepAFileInfo.L_SUMA]	=	Data_Line["Cents"].Trim();				// char[ 16]; // Сумма в копейках
							BFile.Line[CSepAFileInfo.L_DTYPE]	=	"6";							// char[  2]; // Вид документа
							BFile.Line[CSepAFileInfo.L_NDOC]	=	Data_Line["Code"].Trim();				// char[ 10]; // Номер документа
							BFile.Line[CSepAFileInfo.L_CURRENCY]	=	Data_Line["CurrencyId"].Trim();				// char[  3]; // Валюта
							BFile.Line[CSepAFileInfo.L_DATE1]	=	CCommon.DtoC(CCommon.CInt32(Data_Line["DayDate"].Trim())).Substring(2, 6);  // char[  6]; Дата платежа
							BFile.Line[CSepAFileInfo.L_DATE2]	=	CCommon.DtoC(CCommon.CInt32(Data_Line["OrgDate"].Trim())).Substring(2, 6);  // char[  6]; Дата пуступления документа
							BFile.Line[CSepAFileInfo.L_PURPOSE]	=	Data_Line["Purpose"].Trim().Replace("?", "i");		// char[160]; // Назначение платежа
							BFile.Line[CSepAFileInfo.L_SYMBOL]	=	Data_Line["Ctrls"].Trim();				// char[  3]; // Кассовый символ
							BFile.Line[CSepAFileInfo.L_ID]		=	Data_Line["TransferId"].Trim();				// char[  9]; // Идентификатор документа
							BFile.Line[CSepAFileInfo.L_DES]		=	Md5.GetHash(BFile.GetFullLine().Substring(0, 444));	// char[ 64]; // ЕЦП
							BFile.Line[CSepAFileInfo.L_CRLF]	=	CAbc.CRLF;						// char[  2]; // Символ `конец строки`

							if ( !BFile.WriteLine() )
							{
								Data_Header.Close();
								Data_Line.Close();
								BFile.Close();
								return false;
							}
						}
					}
					BFile.Close();
				}
			}
			Data_Line.Close();
		}
		Data_Header.Close();
		return true;
	}

	public	static	bool	WriteVFiles( int DayDate, string DestinationFolder , int VMode )
	{
		CSepVWriter	A	= new	CSepVWriter();
		string	File_Name	=	"!"
					+	( VMode == 0 ? "V" : "W" )
					+	"UUIA"
					+	CCommon.StrY( CCommon.Month( DayDate ) , 1 )
					+	CCommon.StrY( CCommon.Day( DayDate ) , 1  )
					+	"."
					+	CCommon.Right( "0" + CCommon.Hour( CCommon.Clock() ) , 2 )
					+	CCommon.StrY( CCommon.Minute( CCommon.Clock() ) >> 1 , 1 );

		if	( ! A.Create( DestinationFolder + "\\" + File_Name , CAbc.CHARSET_DOS ) )
			return	false;

		CRecordSet Data		= new CRecordSet( Connection1 );
		if	( Data.Open("exec  dbo.pMega_OpenGate_Report;3   @TaskCode='ErcGate', @DateFrom = " + DayDate.ToString() +" , @Mode = " + VMode.ToString() ) )
		{
			if	( ! Data.Read()  )
			{
				A.Head[CSepVFileInfo.H_EMPTYSTR   ]	=	"";				// char[100]  // Пеpвые 100 - пpобелы
				A.Head[CSepVFileInfo.H_CRLF1      ]	=	CAbc.CRLF;			// char[  2]; // Символ концец строки
				A.Head[CSepVFileInfo.H_FILENAME   ]	=	CCommon.Left( File_Name.Trim() , 12 ) ;	// char[ 12]; // Наименование  файла
				A.Head[CSepVFileInfo.H_DATE       ]	=	Now_Date_Str;			// char[  6]; // Дата создания файла
				A.Head[CSepVFileInfo.H_TIME       ]	=	Now_Time_Str;			// char[  4]; // Дата создания файла
				A.Head[CSepVFileInfo.H_STRCOUNT   ]	=	"0";				// char[  6]; // Количество ИС в файле
				A.Head[CSepVFileInfo.H_TOTALDEBET ]	=	"0";				// char[ 16]; // Сумма дебета по файлу
				A.Head[CSepVFileInfo.H_TOTALCREDIT]	=	"0";				// char[ 16]; // Сумма кpедита по файлу
				A.Head[CSepVFileInfo.H_EMPTYSTR2  ]	=	"";				// char[ 64]; // Пустое пространство
				A.Head[CSepVFileInfo.H_CRLF2      ]	=	CAbc.CRLF;			// char[  2]; // Символ конец строки
				if	( ! A.WriteHeader() ) {
					Data.Close();
					A.Close();
					return	false;
				}
			}
			else {
				A.Head[CSepVFileInfo.H_EMPTYSTR   ]	=	"";				// char[100]  // Пеpвые 100 - пpобелы
				A.Head[CSepVFileInfo.H_CRLF1      ]	=	CAbc.CRLF;			// char[  2]; // Символ концец строки
				A.Head[CSepVFileInfo.H_FILENAME   ]	=	CCommon.Left( File_Name.Trim() , 12 ) ;	// char[ 12]; // Наименование  файла
				A.Head[CSepVFileInfo.H_DATE       ]	=	Now_Date_Str;			// char[  6]; // Дата создания файла
				A.Head[CSepVFileInfo.H_TIME       ]	=	Now_Time_Str;			// char[  4]; // Дата создания файла
				A.Head[CSepVFileInfo.H_STRCOUNT   ]	=	Data["TotalLines"].Trim();	// char[  6]; // Количество ИС в файле
				A.Head[CSepVFileInfo.H_TOTALDEBET ]	=	"0";				// char[ 16]; // Сумма дебета по файлу
				A.Head[CSepVFileInfo.H_TOTALCREDIT]	=	Data["TotalCents"].Trim();	// char[ 16]; // Сумма кpедита по файлу
				A.Head[CSepVFileInfo.H_EMPTYSTR2  ]	=	"";				// char[ 64]; // Пустое пространство
				A.Head[CSepVFileInfo.H_CRLF2      ]	=	CAbc.CRLF;			// char[  2]; // Символ конец строки
				if	( ! A.WriteHeader() )
				{
					Data.Close();
					A.Close();
					return	false;
				}
				do
				{
					A.Line[CSepVFileInfo.L_DEBITMFO	]	=	Data["SourceCode"].Trim() ;	// char[  9]; // Дебет-МФО
					A.Line[CSepVFileInfo.L_DEBITACC	]	=	Data["DebitMoniker"].Trim() ;	// char[ 14]; // Дебет-счет
					A.Line[CSepVFileInfo.L_CREDITMFO]	=	Data["TargetCode"].Trim();	// char[  9]; // Кредит-МФО
					A.Line[CSepVFileInfo.L_CREDITACC]	=	Data["CreditMoniker"].Trim() ;	// char[ 14]; // Кредит счет
					A.Line[CSepVFileInfo.L_FLAG	]	=	"1";				// char[  1]; // Флаг `дебет/кредит`
					A.Line[CSepVFileInfo.L_SUMA	]	=	Data["CrncyCents"].Trim();	// char[ 16]; // Сумма в копейках
					A.Line[CSepVFileInfo.L_DTYPE	]	=	Data["Kind"].Trim();		// char[  2]; // Вид документа
					A.Line[CSepVFileInfo.L_NDOC	]	=	Data["DocNum"].Trim();		// char[ 10]; // Номер документа
					A.Line[CSepVFileInfo.L_CURRENCY	]	=	Data["CurrencyId"].Trim();	// char[  3]; // Валюта
					A.Line[CSepVFileInfo.L_DATE1	]	=	CCommon.DtoC(CCommon.CInt32(Data["DayDate"].Trim())).Substring(2, 6) ;	// char[  6]; // Дата платежа
					A.Line[CSepVFileInfo.L_ID	]	=	Data["Id"].Trim();		// char[  9]; // Идентификатор документа
					A.Line[CSepVFileInfo.L_FILENAME1]	=	Data["FileName"].Trim();	// char[ 12]; // Имя файла N 1
					A.Line[CSepVFileInfo.L_LINENUM1	]	=	Data["LineNum"].Trim();		// char[  6]; // Номер строки в файле N 1
					A.Line[CSepVFileInfo.L_FILENAME2]	=	Data["FileName"].Trim();	// char[ 12]; // Имя файла N 2
					A.Line[CSepVFileInfo.L_LINENUM2	]	=	Data["LineNum"].Trim();	// char[  6]; // Номер строки в файле N 2
					A.Line[CSepVFileInfo.L_STATUS	]	=	( ( ( CCommon.CInt32(Data["ProcessFlag"].Trim()) & 3 ) == 3 )  ? "Y" : "N" ); // char[  1]; // Флаг квитовки платежа
					A.Line[CSepVFileInfo.L_TIME	]	=	Now_Time_Str;			// char[  4]; // Время
					A.Line[CSepVFileInfo.L_NOL1	]	=	"0";				// char[  1]; //
					A.Line[CSepVFileInfo.L_NOL2	]	=	"0";				// char[  1]; //
					A.Line[CSepVFileInfo.L_CRLF	]	=	CAbc.CRLF;			// char[  2]; // Символы конца строки
					if	( ! A.WriteLine() ) {
						Data.Close();
						A.Close();
						return	false;
					}
				}	while	( Data.Read() )	;
			}
		}
		Data.Close();
		A.Close();
		return	true;
	}

	public static void Main()
	{
		const	bool	DEBUG		=	false		;
		int		WorkMode	=	0		;// 1 = выгружать B ; 2 = выгружать V
		const	int	WORK_MODE_B	=	1		;// для WorkMode : 1 = выгружать B
		const	int	WORK_MODE_V	=	2		;// для WorkMode : 2 = выгружать V
		int		DayStatus	=	0		;// &1 = стоп по B ; &2 = стоп по V
		int		ErcDate		=	CCommon.Today()	;
		string		TmpDir		=	null		;
		string		StatDir		=	null		;
		string		TodayDir	=	null		;
		string		OutputDir	=	null		;
		string		DataBase	=	null		;
		string		ServerName	=	null		;
		string		ScroogeDir	=	null		;
		string		LogFileName	=	null		;
		string		SimpleFileName	=	null		;
		string		ConfigFileName	=	null		;
		string		ConnectionString=	null		;

		Err.LogToConsole() ;
		CCommon.Print( ""," Создание файлов для ЕРЦ. Версия 3.02 от 23.05.2019г." ) ;

		if	( ! DEBUG )
		{
	                if	( CCommon.ParamCount() < 2 )
	                {
        	        	CCommon.Print(" Режим работы программы (задаются в строке запуска программы):") ;
				CCommon.Print("        /E      -  выполнить выгрузку файлов для ЕРЦ .") ;
				CCommon.Print(" Примеры запуска программы  : ");
				CCommon.Print("        ErcExport  /E");
				CCommon.Print("        ErcExport  /E  2019/05/20") ;
				return	;
        	        }
                	if	( CCommon.Upper( CAbc.ParamStr[1] ).Trim()  != "/E" )
                	{
				CCommon.Print(" Ошибка ! Неправильный режим работы  - " + CAbc.ParamStr[1] );
        	        	return;
			}
			if	( CCommon.ParamCount() > 2 )
			{
				ErcDate	=	CCommon.GetDate( CAbc.ParamStr[2].Trim() );
				if	( ErcDate < 10000 ) {
					CCommon.Print(" Ошибка ! Неправильная дата - " + CAbc.ParamStr[2] );
					return;
				}
			}
		}
		else
			CCommon.Print("--- DEBUG ---" );
		CCommon.Print(" Рабочая дата " +  CCommon.StrD( ErcDate , 10 , 10  ) );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		Scrooge2Config	= new	CScrooge2Config();
		if (!Scrooge2Config.IsValid)
		{
			CCommon.Print( Scrooge2Config.ErrInfo ) ;
			return;
		}

		ScroogeDir	=	(string)Scrooge2Config["Root"];
		ServerName	=	(string)Scrooge2Config["Server"];
		DataBase	=	(string)Scrooge2Config["DataBase"];
		if( ScroogeDir == null )
		{
			CCommon.Print("  Не найдена переменная `Root` в настройках `Скрудж-2` ");
			return;
		}
		if( ServerName == null )
		{
			CCommon.Print("  Не найдена переменная `Server` в настройках `Скрудж-2` ");
			return;
		}
		if( DataBase == null )
		{
			CCommon.Print("  Не найдена переменная `Database` в настройках `Скрудж-2` ");
			return;
		}
		CCommon.Print("  Беру настройки `Скрудж-2` здесь :  " + ScroogeDir );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ConnectionString	=	"Server="	+	ServerName
					+	";Database="	+	DataBase
					+	";Integrated Security=TRUE;"
					;
		Connection1 = new CConnection(ConnectionString);
		Connection2 = new CConnection(ConnectionString);

		if (Connection1.IsOpen())
		{
			CCommon.Print("  Сервер        :  " + ServerName  );
			CCommon.Print("  База данных   :  " + DataBase + CAbc.CRLF );
		}
		else {
			CCommon.Print( CAbc.CRLF + "  Ошибка подключения к серверу !" );
			return;
		}

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CCommand        Command         = new   CCommand( Connection1 );
		DayStatus	=	( int ) CCommon.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;8 @TaskCode = 'ErcGate' , @DayDate = " + ErcDate.ToString() ) , (int) 0 );

		switch	( ( DayStatus & 3 ) )
		{
			case  0:{			// разрешена отправка и B и V
				WorkMode	=	WORK_MODE_B;	// отправлять B
				break;
			}
			case  1:{			// запрещена отправка B ; разрешена отправка V
				WorkMode	=	WORK_MODE_V;	// отправлять V
				break;
			}
			case  2:{			// разрешена отправка B ; запрещена отправка V
				WorkMode	=	WORK_MODE_B;	// отправлять B
				break;
			}
			case  3:{
				CCommon.Print( " На " + CCommon.StrD( ErcDate , 10 , 10  ) + " отправка пачек B и V завершена (см. признак текущего дня)." );
				Connection1.Close();
				Connection2.Close();
				return;
				break;
			}
		}

		SeanceNum	=	( int ) CCommon.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;4  @TaskCode = 'ErcGate' , @ParamCode = 'NumSeance'  , @DayDate = " + ErcDate.ToString() ) , (int) 0 );
		if	( WorkMode == WORK_MODE_B )
		{
			BFileNum	=	( int ) CCommon.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;4  @TaskCode = 'ErcGate' , @ParamCode = 'NumOutFile' , @DayDate = " + ErcDate.ToString() ) , (int) 0 ) ;
			CCommon.Print(" Выполняется формирование B-файлов " ) ;
		}
		else
			CCommon.Print(" Выполняется формирование V и W -файлов " ) ;

		Command.Close();
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ConfigFileName	=	ScroogeDir + CAbc.SLASH + "EXE" + CAbc.SLASH + "GLOBAL.ERC" ;
		ErcConfig = new CErcConfig();
		ErcConfig.Open( ErcDate );
		if ( !ErcConfig.IsValid() )
		{
			CCommon.Print( "  Ошибка чтения настроек из файла " + ConfigFileName );
			System.Console.WriteLine(ErcConfig.ErrInfo())		;
			Connection1.Close();
			Connection2.Close();
			return;
		}
		TodayDir	=	(string)ErcConfig.TodayDir()		;
		StatDir		=	(string)ErcConfig.StatDir()		;
		TmpDir		=	(string)ErcConfig.TmpDir()		;
		OutputDir	=	(string)ErcConfig["OutputDir"]		;
		if ( (TodayDir == null) || (OutputDir == null) )
		{
			CCommon.Print( "  Ошибка чтения настроек из файла " + ConfigFileName );
			Connection1.Close();
			Connection2.Close();
			return;
		}
		TodayDir	=	TodayDir.Trim() ;
		OutputDir	=	OutputDir.Trim();
		StatDir		=	StatDir.Trim();
		if ( (TodayDir == "")  || (OutputDir == "" )  || (StatDir == "" )  )
		{
			CCommon.Print( "  Ошибка чтения настроек из файла " + ConfigFileName );
			Connection1.Close();
			Connection2.Close();
			return;
		}
		if	( ! CCommon.DirExists(StatDir) )
			CCommon.MkDir(StatDir);
		if	( ! CCommon.SaveText( StatDir + "\\" + "test.dat" , "test.dat" , CAbc.CHARSET_DOS ) )
		{
			CCommon.Print( " Ошибка записи в каталог " + StatDir );
			Connection1.Close();
			Connection2.Close();
			return;
		}
		CCommon.DeleteFile(StatDir + "\\" + "test.dat");
		CCommon.Print("  Беру настройки шлюза здесь :  " + ConfigFileName );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		TmpDir=TmpDir+"\\"+SeanceNum.ToString("000000");
		CCommon.MkDir(TmpDir);
		if	( ! CCommon.SaveText( TmpDir + "\\" + "test.dat" , "test.dat" , CAbc.CHARSET_DOS ) )
		{
			CCommon.Print( "  Ошибка записи в каталог " + TmpDir );
			Connection1.Close();
			Connection2.Close();
		}
		LogFileName=ErcConfig.LogDir()+"\\SE"+SeanceNum.ToString("000000")+".TXT";
		if	( ! CCommon.AppendText( LogFileName  , CCommon.Now() + " , " + CCommon.Upper(CCommon.GetUserName()) + CAbc.CRLF, CAbc.CHARSET_DOS ) )
		{
			CCommon.Print( "  Ошибка записи в файл " + LogFileName );
			Connection1.Close();
			Connection2.Close();
		}

		Err.LogTo( LogFileName );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	Отправка B
		if	( WorkMode == WORK_MODE_B )
			if (! WriteBFiles( ErcDate , TmpDir ) )
				CCommon.Print("  Ошибка записи B-файлов!");

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	Отправка V
		if	( WorkMode == WORK_MODE_V )
		{
			WriteVFiles( ErcDate , TmpDir , 0 );
			WriteVFiles( ErcDate , TmpDir , 1 );
			Command         = new   CCommand( Connection1 );
			if	( ! Command.Execute(" exec dbo.pMega_OpenGate_Days;9  @TaskCode = 'ErcGate' , @DayDate = " + ErcDate.ToString() ) ) {
				CCommon.Print( "  Ошибка установки запрета на отправку V-файла!" );
				CCommon.AppendText( LogFileName , "  Ошибка установки запрета на отправку V-файла!" , CAbc.CHARSET_DOS );
				CCommon.AppendText( LogFileName , CAbc.CRLF , CAbc.CHARSET_DOS );
				Connection1.Close();
				Connection2.Close();
				return;
			}
			Command.Close();
		}

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		foreach	( string ResultFile in CCommon.GetFileList( TmpDir+ "\\" + "!*.*" ) )
		{
			if ( ResultFile != null )
			{
				SimpleFileName=CCommon.GetFileName( ResultFile );
				CCommon.CopyFile( ResultFile , TodayDir + "\\" + SimpleFileName );
				CCommon.CopyFile( ResultFile , OutputDir + "\\" + SimpleFileName );
				CCommon.AppendText( LogFileName , "Записываю файл " + SimpleFileName , CAbc.CHARSET_DOS );
				CCommon.Print("  Записываю файл " + SimpleFileName);
			}
		}
		CCommon.AppendText( LogFileName , CAbc.CRLF , CAbc.CHARSET_DOS );
		Connection1.Close();
		Connection2.Close();
	}
}